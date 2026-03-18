using BillingSystem.Core.Enums;

namespace BillingSystem.Core.Entities
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Pending;
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedByAdminId { get; set; }
        public User? ApprovedByAdmin { get; set; }

        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }

    public class Shop : BaseEntity
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
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
        public string? SignaturePath { get; set; }
        public string TermsAndConditions { get; set; } = string.Empty;
    }

    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Supplier : BaseEntity
    {
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }

    public class Product : BaseEntity
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public string Unit { get; set; } = "Pcs"; // Pcs, Kg, Ltr, etc.
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal MRP { get; set; }
        public decimal GstRate { get; set; }       // e.g. 18 for 18%
        public decimal CessRate { get; set; } = 0; // Additional cess %
        public bool IsGstInclusive { get; set; } = false;
        public decimal HsnCode { get; set; }
        public int CurrentStock { get; set; } = 0;
        public int MinStockAlert { get; set; } = 5;
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    }

    public class PurchaseOrder : BaseEntity
    {
        public string PurchaseBillNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public decimal SubTotal { get; set; }
        public decimal TotalGst { get; set; }
        public decimal TotalCess { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }

    public class PurchaseOrderItem : BaseEntity
    {
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal GstRate { get; set; }
        public decimal GstAmount { get; set; }
        public decimal CessRate { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class Bill : BaseEntity
    {
        public string BillNumber { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty; // e.g. "2024-25"

        // Customer Info
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerGstNumber { get; set; } = string.Empty;
        public string CustomerState { get; set; } = string.Empty;

        // Amounts
        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TaxableAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal TotalAmount { get; set; }

        // Payment
        public string PaymentMode { get; set; } = "Cash"; // Cash, UPI, Card, Credit
        public bool IsPaid { get; set; } = true;
        public GstType GstType { get; set; } = GstType.CGST_SGST;

        // PDF
        public string? PdfPath { get; set; }

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public ICollection<BillItem> Items { get; set; } = new List<BillItem>();
    }

    public class BillItem : BaseEntity
    {
        public int BillId { get; set; }
        public Bill Bill { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string ProductName { get; set; } = string.Empty; // Snapshot
        public string HsnCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = "Pcs";
        public decimal SellingPrice { get; set; }
        public decimal MRP { get; set; }
        public decimal DiscountPercent { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
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
}
