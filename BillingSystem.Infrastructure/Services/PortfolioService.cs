using BillingSystem.Core.DTOs.Portfolio;
using BillingSystem.Core.Enums;
using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _context;
        public PortfolioService(ApplicationDbContext context) { _context = context; }

        public async Task<PortfolioSummaryDto> GetPortfolioAsync(PortfolioRequest request)
        {
            var (fromDate, toDate, label) = ResolveDateRange(request);

            var bills = await _context.Bills
                .Include(b => b.Items).ThenInclude(i => i.Product)
                .Where(b => b.BillDate >= fromDate && b.BillDate <= toDate)
                .ToListAsync();

            var purchases = await _context.PurchaseOrders
                .Include(p => p.Items).ThenInclude(i => i.Product)
                .Where(p => p.PurchaseDate >= fromDate && p.PurchaseDate <= toDate)
                .ToListAsync();

            var totalSales = bills.Sum(b => b.TotalAmount);
            var totalPurchases = purchases.Sum(p => p.TotalAmount);
            var cogs = bills.SelectMany(b => b.Items)
                .Sum(i => i.Product != null ? i.Quantity * i.Product.BuyingPrice : 0);
            var grossProfit = (totalSales - bills.Sum(b => b.CgstAmount + b.SgstAmount + b.IgstAmount + b.CessAmount)) - cogs;

            var topProducts = bills.SelectMany(b => b.Items)
                .GroupBy(i => i.ProductName)
                .Select(g => new TopProductDto
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.TotalAmount),
                    Profit = g.Sum(i => i.TotalAmount - (i.Product != null ? i.Quantity * i.Product.BuyingPrice : 0))
                })
                .OrderByDescending(p => p.Revenue).Take(10).ToList();

            var paymentBreakdown = bills
                .GroupBy(b => b.PaymentMode)
                .Select(g => new PaymentModeBreakdown { PaymentMode = g.Key, Count = g.Count(), Amount = g.Sum(b => b.TotalAmount) })
                .OrderByDescending(p => p.Amount).ToList();

            return new PortfolioSummaryDto
            {
                PeriodLabel = label, FromDate = fromDate, ToDate = toDate,
                TotalSales = totalSales, TotalPurchases = totalPurchases,
                GrossProfit = grossProfit,
                TotalDiscount = bills.Sum(b => b.DiscountAmount),
                TotalTaxCollected = bills.Sum(b => b.CgstAmount + b.SgstAmount + b.IgstAmount + b.CessAmount),
                TotalCgst = bills.Sum(b => b.CgstAmount), TotalSgst = bills.Sum(b => b.SgstAmount),
                TotalIgst = bills.Sum(b => b.IgstAmount), TotalCess = bills.Sum(b => b.CessAmount),
                TotalBills = bills.Count,
                TotalItemsSold = bills.SelectMany(b => b.Items).Sum(i => i.Quantity),
                AverageBillValue = bills.Count > 0 ? totalSales / bills.Count : 0,
                Breakdown = BuildBreakdown(bills, purchases, fromDate, toDate, request.Period),
                TopProducts = topProducts, PaymentBreakdown = paymentBreakdown
            };
        }

        private static (DateTime from, DateTime to, string label) ResolveDateRange(PortfolioRequest req)
        {
            var now = DateTime.UtcNow;
            if (req.Period == PortfolioPeriod.Daily)
                return (now.Date, now.Date.AddDays(1).AddSeconds(-1), $"Today ({now:dd MMM yyyy})");
            if (req.Period == PortfolioPeriod.Weekly)
                return (now.AddDays(-6).Date, now.Date.AddDays(1).AddSeconds(-1), "Last 7 Days");
            if (req.Period == PortfolioPeriod.Monthly)
            {
                var ms = new DateTime(now.Year, req.Month ?? now.Month, 1);
                return (ms, ms.AddMonths(1).AddSeconds(-1), ms.ToString("MMMM yyyy"));
            }
            if (req.Period == PortfolioPeriod.Quarterly)
                return GetQuarter(req.Quarter ?? ((now.Month - 1) / 3 + 1), req.Year ?? now.Year);
            if (req.Period == PortfolioPeriod.Yearly)
            {
                int yr = req.Year ?? now.Year;
                return (new DateTime(yr, 4, 1), new DateTime(yr + 1, 3, 31, 23, 59, 59), $"FY {yr}-{(yr + 1) % 100:D2}");
            }
            return (req.FromDate ?? now.AddDays(-30), req.ToDate ?? now, "Custom Range");
        }

        private static (DateTime, DateTime, string) GetQuarter(int q, int year) => q switch
        {
            1 => (new DateTime(year, 4, 1), new DateTime(year, 6, 30, 23, 59, 59), $"Q1 Apr-Jun {year}"),
            2 => (new DateTime(year, 7, 1), new DateTime(year, 9, 30, 23, 59, 59), $"Q2 Jul-Sep {year}"),
            3 => (new DateTime(year, 10, 1), new DateTime(year, 12, 31, 23, 59, 59), $"Q3 Oct-Dec {year}"),
            _ => (new DateTime(year + 1, 1, 1), new DateTime(year + 1, 3, 31, 23, 59, 59), $"Q4 Jan-Mar {year + 1}")
        };

        private static List<PeriodBreakdown> BuildBreakdown(
            List<Core.Entities.Bill> bills,
            List<Core.Entities.PurchaseOrder> purchases,
            DateTime from, DateTime to, PortfolioPeriod period)
        {
            if (period == PortfolioPeriod.Yearly || period == PortfolioPeriod.Custom)
            {
                var months = new List<PeriodBreakdown>();
                var cur = new DateTime(from.Year, from.Month, 1);
                while (cur <= to)
                {
                    var me = cur.AddMonths(1).AddSeconds(-1);
                    var mb = bills.Where(b => b.BillDate >= cur && b.BillDate <= me).ToList();
                    var mp = purchases.Where(p => p.PurchaseDate >= cur && p.PurchaseDate <= me).ToList();
                    var s = mb.Sum(b => b.TotalAmount); var pc = mp.Sum(p => p.TotalAmount);
                    months.Add(new PeriodBreakdown { Label = cur.ToString("MMM yyyy"), Sales = s, Purchases = pc, Profit = s - pc, BillCount = mb.Count });
                    cur = cur.AddMonths(1);
                }
                return months;
            }
            if (period == PortfolioPeriod.Quarterly || period == PortfolioPeriod.Monthly)
            {
                return Enumerable.Range(0, (to - from).Days + 1).Select(i => from.AddDays(i)).Select(d =>
                {
                    var db = bills.Where(b => b.BillDate.Date == d.Date).ToList();
                    var dp = purchases.Where(p => p.PurchaseDate.Date == d.Date).ToList();
                    var s = db.Sum(b => b.TotalAmount); var pc = dp.Sum(p => p.TotalAmount);
                    return new PeriodBreakdown { Label = d.ToString("dd MMM"), Sales = s, Purchases = pc, Profit = s - pc, BillCount = db.Count };
                }).ToList();
            }
            return Enumerable.Range(0, 24).Select(h => new PeriodBreakdown
            {
                Label = $"{h:D2}:00",
                Sales = bills.Where(b => b.BillDate.Hour == h).Sum(b => b.TotalAmount),
                BillCount = bills.Count(b => b.BillDate.Hour == h)
            }).ToList();
        }
    }
}
