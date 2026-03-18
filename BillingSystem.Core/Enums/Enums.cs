namespace BillingSystem.Core.Enums
{
    public enum UserRole
    {
        Admin = 1,
        User = 2
    }

    public enum UserStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Blocked = 3
    }

    public enum GstType
    {
        CGST_SGST = 1,   // Intra-state
        IGST = 2          // Inter-state
    }

    public enum PortfolioPeriod
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Quarterly = 4,
        Yearly = 5,
        Custom = 6
    }

    public enum TaxReportType
    {
        GSTR1 = 1,
        GSTR3B = 2,
        IncomeTax = 3,
        PurchaseSummary = 4,
        SalesSummary = 5
    }
}
