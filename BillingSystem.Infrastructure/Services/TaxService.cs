using BillingSystem.Core.DTOs.Tax;
using BillingSystem.Core.Enums;
using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Services
{
    public class TaxService : ITaxService
    {
        private readonly ApplicationDbContext _context;
        public TaxService(ApplicationDbContext context) { _context = context; }

        public async Task<TaxReportDto> GetTaxReportAsync(TaxReportRequest request)
        {
            var shop = await _context.Shops.FirstOrDefaultAsync();
            var bills = await _context.Bills.Include(b => b.Items)
                .Where(b => b.BillDate >= request.FromDate && b.BillDate <= request.ToDate).ToListAsync();
            var purchases = await _context.PurchaseOrders.Include(p => p.Items).ThenInclude(i => i.Product)
                .Where(p => p.PurchaseDate >= request.FromDate && p.PurchaseDate <= request.ToDate).ToListAsync();

            var dto = new TaxReportDto
            {
                ReportTitle = request.ReportType.ToString(),
                Period = $"{request.FromDate:dd MMM yyyy} to {request.ToDate:dd MMM yyyy}",
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                GstNumber = shop?.GstNumber ?? "",
                PanNumber = shop?.PanNumber ?? "",
                ShopName = shop?.ShopName ?? "",
                TotalTaxableSales = bills.Sum(b => b.TaxableAmount),
                TotalCgstCollected = bills.Sum(b => b.CgstAmount),
                TotalSgstCollected = bills.Sum(b => b.SgstAmount),
                TotalIgstCollected = bills.Sum(b => b.IgstAmount),
                TotalCessCollected = bills.Sum(b => b.CessAmount),
                TotalTaxablePurchases = purchases.Sum(p => p.SubTotal),
                TotalInputCgst = purchases.Sum(p => p.TotalGst / 2),
                TotalInputSgst = purchases.Sum(p => p.TotalGst / 2),
                TotalInputCess = purchases.Sum(p => p.TotalCess),
                SalesLineItems = bills.Select(b => new SalesTaxLineItem
                {
                    BillNumber = b.BillNumber,
                    BillDate = b.BillDate,
                    CustomerName = b.CustomerName,
                    CustomerGstin = b.CustomerGstNumber,
                    TaxableAmount = b.TaxableAmount,
                    CgstAmount = b.CgstAmount,
                    SgstAmount = b.SgstAmount,
                    IgstAmount = b.IgstAmount,
                    CessAmount = b.CessAmount,
                    InvoiceValue = b.TotalAmount
                }).ToList(),
                PurchaseLineItems = purchases.Select(p => new PurchaseTaxLineItem
                {
                    PurchaseBillNumber = p.PurchaseBillNumber,
                    PurchaseDate = p.PurchaseDate,
                    SupplierName = _context.Suppliers.Find(p.SupplierId)?.SupplierName ?? "",
                    SupplierGstin = _context.Suppliers.Find(p.SupplierId)?.GstNumber ?? "",
                    TaxableAmount = p.SubTotal,
                    InputCgst = p.TotalGst / 2,
                    InputSgst = p.TotalGst / 2,
                    InputCess = p.TotalCess,
                    TotalAmount = p.TotalAmount
                }).ToList(),
                RateWiseSummary = bills.SelectMany(b => b.Items)
                    .GroupBy(i => i.GstRate)
                    .Select(g => new GstRateWiseSummary
                    {
                        GstRate = g.Key,
                        TaxableAmount = g.Sum(i => i.TaxableAmount),
                        CgstAmount = g.Sum(i => i.CgstAmount),
                        SgstAmount = g.Sum(i => i.SgstAmount),
                        IgstAmount = g.Sum(i => i.IgstAmount),
                        CessAmount = g.Sum(i => i.CessAmount),
                        TotalTax = g.Sum(i => i.CgstAmount + i.SgstAmount + i.IgstAmount + i.CessAmount)
                    }).OrderBy(r => r.GstRate).ToList()
            };
            return dto;
        }

        public async Task<IncomeTaxSummaryDto> GetIncomeTaxSummaryAsync(int financialYear)
        {
            // FY starts April 1
            var from = new DateTime(financialYear, 4, 1);
            var to = new DateTime(financialYear + 1, 3, 31, 23, 59, 59);

            var bills = await _context.Bills.Where(b => b.BillDate >= from && b.BillDate <= to).ToListAsync();
            var purchases = await _context.PurchaseOrders.Where(p => p.PurchaseDate >= from && p.PurchaseDate <= to).ToListAsync();

            var totalRevenue = bills.Sum(b => b.TotalAmount);
            var totalPurchases = purchases.Sum(p => p.TotalAmount);

            // Build 12 months Apr to Mar
            var months = new List<MonthlyIncomeSummary>();
            for (int m = 0; m < 12; m++)
            {
                var md = from.AddMonths(m);
                var ms = new DateTime(md.Year, md.Month, 1);
                var me = ms.AddMonths(1).AddSeconds(-1);
                var rev = bills.Where(b => b.BillDate >= ms && b.BillDate <= me).Sum(b => b.TotalAmount);
                var pur = purchases.Where(p => p.PurchaseDate >= ms && p.PurchaseDate <= me).Sum(p => p.TotalAmount);
                months.Add(new MonthlyIncomeSummary
                {
                    Month = md.Month,
                    MonthName = md.ToString("MMMM yyyy"),
                    Revenue = rev,
                    Purchases = pur,
                    Profit = rev - pur
                });
            }

            return new IncomeTaxSummaryDto
            {
                FinancialYear = financialYear,
                TotalRevenue = totalRevenue,
                TotalPurchases = totalPurchases,
                GrossProfit = totalRevenue - totalPurchases,
                TotalDiscount = bills.Sum(b => b.DiscountAmount),
                NetProfitBeforeTax = totalRevenue - totalPurchases - bills.Sum(b => b.DiscountAmount),
                MonthlySummary = months
            };
        }

        public async Task<byte[]> ExportTaxReportAsync(TaxReportRequest request)
        {
            var report = await GetTaxReportAsync(request);

            using var workbook = new XLWorkbook();

            // Sheet 1: Sales Summary
            var salesSheet = workbook.Worksheets.Add("Sales (GSTR-1)");
            salesSheet.Cell(1, 1).Value = report.ShopName;
            salesSheet.Cell(1, 1).Style.Font.Bold = true;
            salesSheet.Cell(1, 1).Style.Font.FontSize = 14;
            salesSheet.Cell(2, 1).Value = $"GSTIN: {report.GstNumber}";
            salesSheet.Cell(3, 1).Value = $"Period: {report.Period}";
            salesSheet.Cell(4, 1).Value = "Sales Tax Report (GSTR-1)";
            salesSheet.Cell(4, 1).Style.Font.Bold = true;

            string[] salesHeaders = { "Invoice No", "Date", "Customer Name", "Customer GSTIN", "Taxable Amount", "CGST", "SGST", "IGST", "Cess", "Invoice Value" };
            for (int i = 0; i < salesHeaders.Length; i++)
            {
                salesSheet.Cell(6, i + 1).Value = salesHeaders[i];
                salesSheet.Cell(6, i + 1).Style.Font.Bold = true;
                salesSheet.Cell(6, i + 1).Style.Fill.BackgroundColor = XLColor.Navy;
                salesSheet.Cell(6, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int row = 7;
            foreach (var item in report.SalesLineItems)
            {
                salesSheet.Cell(row, 1).Value = item.BillNumber;
                salesSheet.Cell(row, 2).Value = item.BillDate.ToString("dd-MMM-yyyy");
                salesSheet.Cell(row, 3).Value = item.CustomerName;
                salesSheet.Cell(row, 4).Value = item.CustomerGstin;
                salesSheet.Cell(row, 5).Value = item.TaxableAmount;
                salesSheet.Cell(row, 6).Value = item.CgstAmount;
                salesSheet.Cell(row, 7).Value = item.SgstAmount;
                salesSheet.Cell(row, 8).Value = item.IgstAmount;
                salesSheet.Cell(row, 9).Value = item.CessAmount;
                salesSheet.Cell(row, 10).Value = item.InvoiceValue;
                row++;
            }

            // Totals row
            salesSheet.Cell(row, 1).Value = "TOTAL";
            salesSheet.Cell(row, 1).Style.Font.Bold = true;
            salesSheet.Cell(row, 5).Value = report.TotalTaxableSales;
            salesSheet.Cell(row, 6).Value = report.TotalCgstCollected;
            salesSheet.Cell(row, 7).Value = report.TotalSgstCollected;
            salesSheet.Cell(row, 8).Value = report.TotalIgstCollected;
            salesSheet.Cell(row, 9).Value = report.TotalCessCollected;
            salesSheet.Cell(row, 10).Value = report.SalesLineItems.Sum(s => s.InvoiceValue);
            for (int c = 1; c <= 10; c++) salesSheet.Cell(row, c).Style.Font.Bold = true;

            salesSheet.Columns().AdjustToContents();

            // Sheet 2: Purchase Summary
            var purSheet = workbook.Worksheets.Add("Purchases (GSTR-2)");
            purSheet.Cell(1, 1).Value = "Purchase Tax Report (GSTR-2)";
            purSheet.Cell(1, 1).Style.Font.Bold = true;
            purSheet.Cell(2, 1).Value = $"Period: {report.Period}";

            string[] purHeaders = { "Bill No", "Date", "Supplier Name", "Supplier GSTIN", "Taxable Amount", "Input CGST", "Input SGST", "Input IGST", "Input Cess", "Total Amount" };
            for (int i = 0; i < purHeaders.Length; i++)
            {
                purSheet.Cell(4, i + 1).Value = purHeaders[i];
                purSheet.Cell(4, i + 1).Style.Font.Bold = true;
                purSheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.DarkGreen;
                purSheet.Cell(4, i + 1).Style.Font.FontColor = XLColor.White;
            }

            row = 5;
            foreach (var item in report.PurchaseLineItems)
            {
                purSheet.Cell(row, 1).Value = item.PurchaseBillNumber;
                purSheet.Cell(row, 2).Value = item.PurchaseDate.ToString("dd-MMM-yyyy");
                purSheet.Cell(row, 3).Value = item.SupplierName;
                purSheet.Cell(row, 4).Value = item.SupplierGstin;
                purSheet.Cell(row, 5).Value = item.TaxableAmount;
                purSheet.Cell(row, 6).Value = item.InputCgst;
                purSheet.Cell(row, 7).Value = item.InputSgst;
                purSheet.Cell(row, 8).Value = item.InputIgst;
                purSheet.Cell(row, 9).Value = item.InputCess;
                purSheet.Cell(row, 10).Value = item.TotalAmount;
                row++;
            }
            purSheet.Columns().AdjustToContents();

            // Sheet 3: GST Rate-wise Summary
            var rateSheet = workbook.Worksheets.Add("Rate-wise Summary");
            rateSheet.Cell(1, 1).Value = "GST Rate-wise Summary";
            rateSheet.Cell(1, 1).Style.Font.Bold = true;
            string[] rateHeaders = { "GST Rate %", "Taxable Amount", "CGST", "SGST", "IGST", "Cess", "Total Tax" };
            for (int i = 0; i < rateHeaders.Length; i++)
            {
                rateSheet.Cell(3, i + 1).Value = rateHeaders[i];
                rateSheet.Cell(3, i + 1).Style.Font.Bold = true;
                rateSheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.DarkSlateBlue;
                rateSheet.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
            }
            row = 4;
            foreach (var r in report.RateWiseSummary)
            {
                rateSheet.Cell(row, 1).Value = r.GstRate;
                rateSheet.Cell(row, 2).Value = r.TaxableAmount;
                rateSheet.Cell(row, 3).Value = r.CgstAmount;
                rateSheet.Cell(row, 4).Value = r.SgstAmount;
                rateSheet.Cell(row, 5).Value = r.IgstAmount;
                rateSheet.Cell(row, 6).Value = r.CessAmount;
                rateSheet.Cell(row, 7).Value = r.TotalTax;
                row++;
            }
            rateSheet.Columns().AdjustToContents();

            // Sheet 4: Tax Summary
            var summarySheet = workbook.Worksheets.Add("Tax Summary");
            summarySheet.Cell(1, 1).Value = "GST Summary (GSTR-3B)";
            summarySheet.Cell(1, 1).Style.Font.Bold = true;
            summarySheet.Cell(1, 1).Style.Font.FontSize = 14;

            var summaryData = new (string Label, decimal Value)[]
            {
                ("OUTPUT TAX (Sales)", 0),
                ("Total Taxable Sales", report.TotalTaxableSales),
                ("CGST Collected", report.TotalCgstCollected),
                ("SGST Collected", report.TotalSgstCollected),
                ("IGST Collected", report.TotalIgstCollected),
                ("Cess Collected", report.TotalCessCollected),
                ("Total Output Tax", report.TotalOutputTax),
                ("", 0),
                ("INPUT TAX CREDIT (Purchases)", 0),
                ("Total Taxable Purchases", report.TotalTaxablePurchases),
                ("Input CGST", report.TotalInputCgst),
                ("Input SGST", report.TotalInputSgst),
                ("Input IGST", report.TotalInputIgst),
                ("Input Cess", report.TotalInputCess),
                ("Total Input Tax Credit", report.TotalInputTax),
                ("", 0),
                ("NET TAX PAYABLE", report.NetTaxPayable)
            };

            row = 3;
            foreach (var (lbl, val) in summaryData)
            {
                summarySheet.Cell(row, 1).Value = lbl;
                if (val != 0) summarySheet.Cell(row, 2).Value = val;
                if (lbl.Contains("OUTPUT") || lbl.Contains("INPUT") || lbl.Contains("NET"))
                    summarySheet.Cell(row, 1).Style.Font.Bold = true;
                row++;
            }
            summarySheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
