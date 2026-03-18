using BillingSystem.Core.Enums;

namespace BillingSystem.Core.DTOs.Bill
{
    public class CreateBillRequest
    {
        public DateTime BillDate { get; set; } = DateTime.UtcNow;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerGstNumber { get; set; } = string.Empty;
        public string CustomerState { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; } = 0;
        public string PaymentMode { get; set; } = "Cash";
        public bool IsPaid { get; set; } = true;
        public GstType GstType { get; set; } = GstType.CGST_SGST;
        public List<BillItemRequest> Items { get; set; } = new();
    }

    public class BillItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPercent { get; set; } = 0;
    }

    public class BillDto
    {
        public int Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerGstNumber { get; set; } = string.Empty;
        public string CustomerState { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public string GstType { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PdfPath { get; set; }
        public List<BillItemDto> Items { get; set; } = new();

        // Shop details for PDF
        public ShopDetailsDto? ShopDetails { get; set; }
    }

    public class BillItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string HsnCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal MRP { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal GstRate { get; set; }
        public decimal CgstRate { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstRate { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstRate { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessRate { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ShopDetailsDto
    {
        public string ShopName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public string TermsAndConditions { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
    }

    public class BillListDto
    {
        public int Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool HasPdf { get; set; }
    }

    public class BillFilterRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? CustomerName { get; set; }
        public string? PaymentMode { get; set; }
        public bool? IsPaid { get; set; }
        public int? CreatedByUserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
