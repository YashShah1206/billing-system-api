using BillingSystem.Core.DTOs.Bill;
using BillingSystem.Core.Entities;
using BillingSystem.Core.Enums;
using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BillingSystem.Infrastructure.Services
{
    public class BillService : IBillService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public BillService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<BillDto> CreateBillAsync(CreateBillRequest request, int userId)
        {
            var billNumber = await GenerateBillNumberAsync();
            var financialYear = GetFinancialYear(request.BillDate);

            var bill = new Bill
            {
                BillNumber = billNumber,
                BillDate = request.BillDate,
                FinancialYear = financialYear,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                CustomerEmail = request.CustomerEmail,
                CustomerAddress = request.CustomerAddress,
                CustomerGstNumber = request.CustomerGstNumber,
                CustomerState = request.CustomerState,
                DiscountPercent = request.DiscountPercent,
                PaymentMode = request.PaymentMode,
                IsPaid = request.IsPaid,
                GstType = request.GstType,
                CreatedByUserId = userId
            };

            decimal subTotal = 0, totalTaxable = 0;
            decimal totalCgst = 0, totalSgst = 0, totalIgst = 0, totalCess = 0, totalDiscount = 0;

            foreach (var itemReq in request.Items)
            {
                var product = await _context.Products.FindAsync(itemReq.ProductId)
                    ?? throw new KeyNotFoundException($"Product {itemReq.ProductId} not found.");

                if (product.CurrentStock < itemReq.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for {product.ProductName}. Available: {product.CurrentStock}");

                var sellingPrice = itemReq.SellingPrice > 0 ? itemReq.SellingPrice : product.SellingPrice;
                var lineSubTotal = sellingPrice * itemReq.Quantity;
                var discountAmt = Math.Round(lineSubTotal * itemReq.DiscountPercent / 100, 2);
                var taxableAmt = lineSubTotal - discountAmt;

                if (request.DiscountPercent > 0)
                {
                    var billDisc = Math.Round(taxableAmt * request.DiscountPercent / 100, 2);
                    discountAmt += billDisc;
                    taxableAmt -= billDisc;
                }

                taxableAmt = Math.Round(taxableAmt, 2);

                decimal cgstAmt = 0, sgstAmt = 0, igstAmt = 0, cessAmt = 0;
                // Store clean rates (e.g. 14 not 14.000000)
                decimal halfGst = Math.Round(product.GstRate / 2, 2);

                if (request.GstType == GstType.CGST_SGST)
                {
                    cgstAmt = Math.Round(taxableAmt * halfGst / 100, 2);
                    sgstAmt = Math.Round(taxableAmt * halfGst / 100, 2);
                }
                else
                {
                    igstAmt = Math.Round(taxableAmt * product.GstRate / 100, 2);
                }
                cessAmt = Math.Round(taxableAmt * product.CessRate / 100, 2);

                var lineTotal = taxableAmt + cgstAmt + sgstAmt + igstAmt + cessAmt;

                bill.Items.Add(new BillItem
                {
                    ProductId    = itemReq.ProductId,
                    ProductName  = product.ProductName,
                    HsnCode      = ((long)product.HsnCode).ToString(),   // no decimals
                    Quantity     = itemReq.Quantity,
                    Unit         = product.Unit,
                    SellingPrice = Math.Round(sellingPrice, 2),
                    MRP          = Math.Round(product.MRP, 2),
                    DiscountPercent = Math.Round(itemReq.DiscountPercent, 2),
                    DiscountAmount  = Math.Round(discountAmt, 2),
                    TaxableAmount   = taxableAmt,
                    GstRate  = Math.Round(product.GstRate, 2),
                    CgstRate = halfGst,
                    CgstAmount = cgstAmt,
                    SgstRate = halfGst,
                    SgstAmount = sgstAmt,
                    IgstRate   = request.GstType == GstType.IGST ? Math.Round(product.GstRate, 2) : 0,
                    IgstAmount = igstAmt,
                    CessRate   = Math.Round(product.CessRate, 2),
                    CessAmount = cessAmt,
                    TotalAmount = Math.Round(lineTotal, 2)
                });

                product.CurrentStock -= itemReq.Quantity;
                product.UpdatedAt = DateTime.UtcNow;

                subTotal      += lineSubTotal;
                totalTaxable  += taxableAmt;
                totalCgst     += cgstAmt;
                totalSgst     += sgstAmt;
                totalIgst     += igstAmt;
                totalCess     += cessAmt;
                totalDiscount += discountAmt;
            }

            bill.SubTotal       = Math.Round(subTotal, 2);
            bill.DiscountAmount = Math.Round(totalDiscount, 2);
            bill.DiscountPercent = Math.Round(request.DiscountPercent, 2);
            bill.TaxableAmount  = Math.Round(totalTaxable, 2);
            bill.CgstAmount     = Math.Round(totalCgst, 2);
            bill.SgstAmount     = Math.Round(totalSgst, 2);
            bill.IgstAmount     = Math.Round(totalIgst, 2);
            bill.CessAmount     = Math.Round(totalCess, 2);

            var totalBeforeRound = totalTaxable + totalCgst + totalSgst + totalIgst + totalCess;
            bill.TotalAmount = Math.Round(totalBeforeRound);
            bill.RoundOff    = Math.Round(bill.TotalAmount - totalBeforeRound, 2);

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return await GetBillByIdAsync(bill.Id);
        }

        public async Task<BillDto> GetBillByIdAsync(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.CreatedByUser)
                .Include(b => b.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new KeyNotFoundException("Bill not found.");

            var shop = await _context.Shops.FirstOrDefaultAsync();
            return MapBillToDto(bill, shop);
        }

        public async Task<PagedResult<BillListDto>> GetAllBillsAsync(BillFilterRequest filter)
        {
            var query = _context.Bills.Include(b => b.CreatedByUser).AsQueryable();

            if (filter.FromDate.HasValue)
                query = query.Where(b => b.BillDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(b => b.BillDate <= filter.ToDate.Value);
            if (!string.IsNullOrWhiteSpace(filter.CustomerName))
                query = query.Where(b => b.CustomerName.Contains(filter.CustomerName));
            if (!string.IsNullOrWhiteSpace(filter.PaymentMode))
                query = query.Where(b => b.PaymentMode == filter.PaymentMode);
            if (filter.IsPaid.HasValue)
                query = query.Where(b => b.IsPaid == filter.IsPaid.Value);
            if (filter.CreatedByUserId.HasValue)
                query = query.Where(b => b.CreatedByUserId == filter.CreatedByUserId.Value);

            var total = await query.CountAsync();
            var bills = await query
                .OrderByDescending(b => b.BillDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<BillListDto>
            {
                Data = bills.Select(b => new BillListDto
                {
                    Id           = b.Id,
                    BillNumber   = b.BillNumber,
                    BillDate     = b.BillDate,
                    CustomerName = b.CustomerName,
                    CustomerPhone = b.CustomerPhone,
                    TotalAmount  = b.TotalAmount,
                    PaymentMode  = b.PaymentMode,
                    IsPaid       = b.IsPaid,
                    CreatedBy    = b.CreatedByUser?.FullName ?? "",
                    HasPdf       = !string.IsNullOrEmpty(b.PdfPath)
                }).ToList(),
                TotalCount = total,
                Page       = filter.Page,
                PageSize   = filter.PageSize
            };
        }

        public async Task<byte[]> GenerateBillPdfAsync(int billId)
        {
            var billDto = await GetBillByIdAsync(billId);
            QuestPDF.Settings.License = LicenseType.Community;
            return CreatePdfDocument(billDto).GeneratePdf();
        }

        public async Task<string> SaveBillPdfAsync(int billId)
        {
            var bill = await _context.Bills.FindAsync(billId)
                ?? throw new KeyNotFoundException("Bill not found.");

            var pdfBytes = await GenerateBillPdfAsync(billId);

            var baseDir = _configuration["BillStorage:BasePath"] ?? "Bills";
            var date    = bill.BillDate;
            var folder  = Path.Combine(baseDir,
                              date.Year.ToString(),
                              date.ToString("MM-MMMM"),
                              date.ToString("dd"));
            Directory.CreateDirectory(folder);

            var safeName = string.Join("_",
                bill.CustomerName.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{bill.BillNumber}_{safeName}_{date:yyyyMMdd}.pdf";
            var filePath = Path.Combine(folder, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            bill.PdfPath   = filePath;
            bill.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return filePath;
        }

        // ══════════════════════════════════════════════════════════
        //  PDF GENERATION
        // ══════════════════════════════════════════════════════════
        private static IDocument CreatePdfDocument(BillDto b)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.2f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(h => ComposeHeader(h, b));
                    page.Content().Element(c => ComposeContent(c, b));
                    page.Footer().Element(f => ComposeFooter(f, b));
                });
            });
        }

        // ── HEADER ───────────────────────────────────────────────
        private static void ComposeHeader(IContainer header, BillDto b)
        {
            header.Column(col =>
            {
                // Shop banner
                col.Item().Background("#1a237e").Padding(14).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(b.ShopDetails?.ShopName ?? "SHOP")
                            .FontSize(20).Bold().FontColor("#ffffff");
                        c.Item().Text(b.ShopDetails?.Address ?? "")
                            .FontColor("#b3c5f9").FontSize(8);
                        c.Item().Text(
                            $"{b.ShopDetails?.City}, {b.ShopDetails?.State} - {b.ShopDetails?.PinCode}")
                            .FontColor("#b3c5f9").FontSize(8);
                        c.Item().Text(
                            $"Ph: {b.ShopDetails?.PhoneNumber}   |   Email: {b.ShopDetails?.Email}")
                            .FontColor("#b3c5f9").FontSize(8);
                    });

                    row.ConstantItem(165).AlignRight().Column(c =>
                    {
                        c.Item().Text("TAX INVOICE")
                            .FontSize(14).Bold().FontColor("#ffd740").AlignRight();
                        c.Item().Text($"GSTIN: {b.ShopDetails?.GstNumber}")
                            .FontColor("#b3c5f9").FontSize(8).AlignRight();
                        c.Item().Text($"PAN: {b.ShopDetails?.PanNumber}")
                            .FontColor("#b3c5f9").FontSize(8).AlignRight();
                    });
                });

                // Bill meta + customer
                col.Item().Background("#e8eaf6").Padding(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("Invoice No: ").Bold();
                            t.Span(b.BillNumber);
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("Date: ").Bold();
                            t.Span(b.BillDate.ToString("dd-MMM-yyyy"));
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("Financial Year: ").Bold();
                            t.Span(b.FinancialYear);
                        });
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("BILL TO:").Bold().FontColor("#1a237e");
                        c.Item().Text(b.CustomerName).Bold();
                        if (!string.IsNullOrEmpty(b.CustomerAddress))
                            c.Item().Text(b.CustomerAddress).FontSize(8);
                        c.Item().Text(b.CustomerPhone);
                        if (!string.IsNullOrEmpty(b.CustomerGstNumber))
                            c.Item().Text(t =>
                            {
                                t.Span("GSTIN: ").Bold();
                                t.Span(b.CustomerGstNumber);
                            });
                    });
                });
            });
        }

        // ── CONTENT ──────────────────────────────────────────────
        private static void ComposeContent(IContainer content, BillDto b)
        {
            content.Column(col =>
            {
                // ── Items table ──
                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22);   // #
                        cols.RelativeColumn(3);    // Product
                        cols.ConstantColumn(46);   // HSN
                        cols.ConstantColumn(30);   // Qty
                        cols.ConstantColumn(28);   // Unit
                        cols.ConstantColumn(55);   // Rate
                        cols.ConstantColumn(38);   // Disc%
                        cols.ConstantColumn(55);   // Taxable
                        cols.ConstantColumn(68);   // GST
                        cols.ConstantColumn(36);   // Cess
                        cols.ConstantColumn(58);   // Total
                    });

                    // Header row
                    table.Header(h =>
                    {
                        string gstCol = b.GstType == "CGST_SGST" ? "CGST+SGST" : "IGST";
                        foreach (var title in new[]
                            { "#", "Product", "HSN", "Qty", "Unit",
                              "Rate(₹)", "Disc%", "Taxable", gstCol, "Cess", "Total(₹)" })
                        {
                            h.Cell().Background("#1a237e").Padding(4)
                                .Text(title).FontColor("#ffffff").Bold().FontSize(8);
                        }
                    });

                    int sr = 1;
                    foreach (var item in b.Items)
                    {
                        var bg = sr % 2 == 0 ? "#f3f4f9" : "#ffffff";

                        // format helper
                        string F2(decimal v)  => v.ToString("N2");
                        string Pct(decimal v) => v.ToString("0.##") + "%";

                        table.Cell().Background(bg).Padding(4).Text(sr.ToString());
                        table.Cell().Background(bg).Padding(4).Text(item.ProductName);
                        table.Cell().Background(bg).Padding(4).Text(item.HsnCode);
                        table.Cell().Background(bg).Padding(4).AlignRight()
                            .Text(item.Quantity.ToString());
                        table.Cell().Background(bg).Padding(4).Text(item.Unit);
                        table.Cell().Background(bg).Padding(4).AlignRight()
                            .Text(F2(item.SellingPrice));
                        table.Cell().Background(bg).Padding(4)
                            .Text(item.DiscountPercent > 0 ? Pct(item.DiscountPercent) : "-");
                        table.Cell().Background(bg).Padding(4).AlignRight()
                            .Text(F2(item.TaxableAmount));

                        // GST cell
                        if (b.GstType == "CGST_SGST")
                            table.Cell().Background(bg).Padding(3).Column(c =>
                            {
                                c.Item().Text(
                                    $"C {Pct(item.CgstRate)}: {F2(item.CgstAmount)}")
                                    .FontSize(7);
                                c.Item().Text(
                                    $"S {Pct(item.SgstRate)}: {F2(item.SgstAmount)}")
                                    .FontSize(7);
                            });
                        else
                            table.Cell().Background(bg).Padding(4)
                                .Text($"IGST {Pct(item.IgstRate)}: {F2(item.IgstAmount)}")
                                .FontSize(7);

                        table.Cell().Background(bg).Padding(4)
                            .Text(item.CessAmount > 0
                                ? $"{Pct(item.CessRate)}: {F2(item.CessAmount)}"
                                : "-")
                            .FontSize(7);
                        table.Cell().Background(bg).Padding(4).AlignRight()
                            .Text(F2(item.TotalAmount)).Bold();

                        sr++;
                    }
                });

                // ── Terms + Totals row ──
                col.Item().PaddingTop(10).Row(row =>
                {
                    // Left side: terms
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Terms & Conditions:").Bold().FontSize(8);
                        c.Item().Text(b.ShopDetails?.TermsAndConditions ?? "")
                            .FontSize(7).FontColor("#555555");
                        c.Item().PaddingTop(6).Text(t =>
                        {
                            t.Span("Payment Mode: ").Bold();
                            t.Span(b.PaymentMode);
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("Status: ").Bold();
                            t.Span(b.IsPaid ? "PAID" : "UNPAID")
                                .FontColor(b.IsPaid ? "#2e7d32" : "#c62828").Bold();
                        });
                    });

                    // Right side: totals
                    row.ConstantItem(210).Column(c =>
                    {
                        // helper renders one row of the totals box
                        void TRow(IContainer cell, string label, string value,
                            bool isBold = false, string vColor = "#222222")
                        {
                            cell.Background(isBold ? "#1a237e" : "#f3f4f9")
                                .Padding(5)
                                .Row(r =>
                                {
                                    var lt = r.RelativeItem().Text(label)
                                        .FontColor(isBold ? "#ffffff" : "#333333");
                                    if (isBold) lt.Bold();

                                    var vt = r.ConstantItem(85).AlignRight()
                                        .Text(value)
                                        .FontColor(isBold ? "#ffd740" : vColor);
                                    if (isBold) vt.Bold();
                                });
                        }

                        string Cur(decimal v) => $"₹{v:N2}";
                        string Pct(decimal v) => v.ToString("0.##") + "%";

                        c.Item().Element(e =>
                            TRow(e, "Sub Total:", Cur(b.SubTotal)));

                        if (b.DiscountAmount > 0)
                            c.Item().Element(e =>
                                TRow(e,
                                    $"Discount ({Pct(b.DiscountPercent)}):",
                                    Cur(b.DiscountAmount),
                                    vColor: "#c62828"));

                        c.Item().Element(e =>
                            TRow(e, "Taxable Amount:", Cur(b.TaxableAmount)));

                        if (b.CgstAmount > 0)
                            c.Item().Element(e =>
                                TRow(e, "CGST:", Cur(b.CgstAmount)));

                        if (b.SgstAmount > 0)
                            c.Item().Element(e =>
                                TRow(e, "SGST:", Cur(b.SgstAmount)));

                        if (b.IgstAmount > 0)
                            c.Item().Element(e =>
                                TRow(e, "IGST:", Cur(b.IgstAmount)));

                        if (b.CessAmount > 0)
                            c.Item().Element(e =>
                                TRow(e, "Cess:", Cur(b.CessAmount)));

                        if (b.RoundOff != 0)
                            c.Item().Element(e =>
                                TRow(e, "Round Off:", Cur(b.RoundOff)));

                        c.Item().Element(e =>
                            TRow(e, "TOTAL AMOUNT:", Cur(b.TotalAmount), isBold: true));
                    });
                });

                // ── Amount in words ──
                col.Item().PaddingTop(8).Background("#e8eaf6").Padding(8).Text(t =>
                {
                    t.Span("Amount in Words: ").Bold();
                    t.Span(NumberToWords((long)b.TotalAmount) + " Rupees Only");
                });
            });
        }

        // ── FOOTER ───────────────────────────────────────────────
        private static void ComposeFooter(IContainer footer, BillDto b)
        {
            footer.Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("This is a computer-generated invoice.")
                        .FontSize(7).FontColor("#999999");
                    c.Item().Text($"Generated on: {DateTime.Now:dd-MMM-yyyy HH:mm}")
                        .FontSize(7).FontColor("#999999");
                });

                row.ConstantItem(150).Column(c =>
                {
                    c.Item().PaddingTop(18)
                        .BorderTop(1).BorderColor("#cccccc")
                        .Text("Authorised Signatory")
                        .AlignCenter().FontSize(8);
                    c.Item().Text(b.ShopDetails?.OwnerName ?? "")
                        .AlignCenter().Bold().FontSize(8);
                });
            });
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════
        private async Task<string> GenerateBillNumberAsync()
        {
            var fy = GetFinancialYear(DateTime.UtcNow);
            var last = await _context.Bills
                .Where(b => b.FinancialYear == fy)
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (last != null)
            {
                var parts = last.BillNumber.Split('-');
                if (parts.Length > 0 && int.TryParse(parts[^1], out int n)) seq = n + 1;
            }
            return $"INV-{fy.Replace("-", "")}-{seq:D5}";
        }

        private static string GetFinancialYear(DateTime date)
        {
            int y = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{y}-{(y + 1) % 100:D2}";
        }

        private static BillDto MapBillToDto(Bill b, Core.Entities.Shop? shop) => new()
        {
            Id            = b.Id,
            BillNumber    = b.BillNumber,
            BillDate      = b.BillDate,
            FinancialYear = b.FinancialYear,
            CustomerName  = b.CustomerName,
            CustomerPhone = b.CustomerPhone,
            CustomerEmail = b.CustomerEmail,
            CustomerAddress    = b.CustomerAddress,
            CustomerGstNumber  = b.CustomerGstNumber,
            CustomerState      = b.CustomerState,
            SubTotal       = b.SubTotal,
            DiscountPercent = b.DiscountPercent,
            DiscountAmount  = b.DiscountAmount,
            TaxableAmount   = b.TaxableAmount,
            CgstAmount      = b.CgstAmount,
            SgstAmount      = b.SgstAmount,
            IgstAmount      = b.IgstAmount,
            CessAmount      = b.CessAmount,
            RoundOff        = b.RoundOff,
            TotalAmount     = b.TotalAmount,
            PaymentMode     = b.PaymentMode,
            IsPaid          = b.IsPaid,
            GstType         = b.GstType.ToString(),
            CreatedBy       = b.CreatedByUser?.FullName ?? "",
            CreatedByUserId = b.CreatedByUserId,
            CreatedAt       = b.CreatedAt,
            PdfPath         = b.PdfPath,
            Items = b.Items.Select(i => new BillItemDto
            {
                Id          = i.Id,
                ProductId   = i.ProductId,
                ProductName = i.ProductName,
                HsnCode     = i.HsnCode,
                Quantity    = i.Quantity,
                Unit        = i.Unit,
                SellingPrice    = i.SellingPrice,
                MRP             = i.MRP,
                DiscountPercent = i.DiscountPercent,
                DiscountAmount  = i.DiscountAmount,
                TaxableAmount   = i.TaxableAmount,
                GstRate    = i.GstRate,
                CgstRate   = i.CgstRate,
                CgstAmount = i.CgstAmount,
                SgstRate   = i.SgstRate,
                SgstAmount = i.SgstAmount,
                IgstRate   = i.IgstRate,
                IgstAmount = i.IgstAmount,
                CessRate   = i.CessRate,
                CessAmount = i.CessAmount,
                TotalAmount = i.TotalAmount
            }).ToList(),
            ShopDetails = shop == null ? null : new ShopDetailsDto
            {
                ShopName  = shop.ShopName,
                OwnerName = shop.OwnerName,
                Address   = shop.Address,
                City      = shop.City,
                State     = shop.State,
                PinCode   = shop.PinCode,
                PhoneNumber = shop.PhoneNumber,
                Email       = shop.Email,
                GstNumber   = shop.GstNumber,
                PanNumber   = shop.PanNumber,
                TermsAndConditions = shop.TermsAndConditions,
                LogoPath    = shop.LogoPath
            }
        };

        private static string NumberToWords(long number)
        {
            if (number == 0) return "Zero";
            string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven",
                "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen",
                "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty",
                "Sixty", "Seventy", "Eighty", "Ninety" };

            if (number < 0)       return "Minus " + NumberToWords(-number);
            if (number < 20)      return ones[number];
            if (number < 100)     return tens[number / 10] + (number % 10 != 0 ? " " + ones[number % 10] : "");
            if (number < 1000)    return ones[number / 100] + " Hundred" + (number % 100 != 0 ? " " + NumberToWords(number % 100) : "");
            if (number < 100000)  return NumberToWords(number / 1000) + " Thousand" + (number % 1000 != 0 ? " " + NumberToWords(number % 1000) : "");
            if (number < 10000000) return NumberToWords(number / 100000) + " Lakh" + (number % 100000 != 0 ? " " + NumberToWords(number % 100000) : "");
            return NumberToWords(number / 10000000) + " Crore" + (number % 10000000 != 0 ? " " + NumberToWords(number % 10000000) : "");
        }
    }
}
