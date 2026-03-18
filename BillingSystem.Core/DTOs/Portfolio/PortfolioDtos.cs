using BillingSystem.Core.Enums;

namespace BillingSystem.Core.DTOs.Portfolio
{
    public class PortfolioRequest
    {
        public PortfolioPeriod Period { get; set; } = PortfolioPeriod.Monthly;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? Quarter { get; set; } // 1,2,3,4
    }

    public class PortfolioSummaryDto
    {
        public string PeriodLabel { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalTaxCollected { get; set; }
        public decimal TotalCgst { get; set; }
        public decimal TotalSgst { get; set; }
        public decimal TotalIgst { get; set; }
        public decimal TotalCess { get; set; }
        public int TotalBills { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal AverageBillValue { get; set; }
        public decimal ProfitMarginPercent => TotalSales > 0 ? (GrossProfit / TotalSales) * 100 : 0;

        public List<PeriodBreakdown> Breakdown { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<PaymentModeBreakdown> PaymentBreakdown { get; set; } = new();
    }

    public class PeriodBreakdown
    {
        public string Label { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal Purchases { get; set; }
        public decimal Profit { get; set; }
        public int BillCount { get; set; }
    }

    public class TopProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
    }

    public class PaymentModeBreakdown
    {
        public string PaymentMode { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}

namespace BillingSystem.Core.DTOs.Tax
{
    public class TaxReportRequest
    {
        public TaxReportType ReportType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class TaxReportDto
    {
        public string ReportTitle { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string GstNumber { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;

        // Sales Summary
        public decimal TotalTaxableSales { get; set; }
        public decimal TotalExemptSales { get; set; }
        public decimal TotalCgstCollected { get; set; }
        public decimal TotalSgstCollected { get; set; }
        public decimal TotalIgstCollected { get; set; }
        public decimal TotalCessCollected { get; set; }
        public decimal TotalOutputTax => TotalCgstCollected + TotalSgstCollected + TotalIgstCollected + TotalCessCollected;

        // Purchase Summary
        public decimal TotalTaxablePurchases { get; set; }
        public decimal TotalInputCgst { get; set; }
        public decimal TotalInputSgst { get; set; }
        public decimal TotalInputIgst { get; set; }
        public decimal TotalInputCess { get; set; }
        public decimal TotalInputTax => TotalInputCgst + TotalInputSgst + TotalInputIgst + TotalInputCess;

        // Net Tax
        public decimal NetTaxPayable => TotalOutputTax - TotalInputTax;

        public List<GstRateWiseSummary> RateWiseSummary { get; set; } = new();
        public List<SalesTaxLineItem> SalesLineItems { get; set; } = new();
        public List<PurchaseTaxLineItem> PurchaseLineItems { get; set; } = new();
    }

    public class GstRateWiseSummary
    {
        public decimal GstRate { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalTax { get; set; }
    }

    public class SalesTaxLineItem
    {
        public string BillNumber { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerGstin { get; set; } = string.Empty;
        public decimal TaxableAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal InvoiceValue { get; set; }
    }

    public class PurchaseTaxLineItem
    {
        public string PurchaseBillNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierGstin { get; set; } = string.Empty;
        public decimal TaxableAmount { get; set; }
        public decimal InputCgst { get; set; }
        public decimal InputSgst { get; set; }
        public decimal InputIgst { get; set; }
        public decimal InputCess { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class IncomeTaxSummaryDto
    {
        public int FinancialYear { get; set; } // e.g. 2024 means 2024-25
        public decimal TotalRevenue { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal NetProfitBeforeTax { get; set; }
        public List<MonthlyIncomeSummary> MonthlySummary { get; set; } = new();
    }

    public class MonthlyIncomeSummary
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Purchases { get; set; }
        public decimal Profit { get; set; }
    }
}
