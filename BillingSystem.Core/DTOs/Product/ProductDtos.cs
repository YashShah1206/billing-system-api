namespace BillingSystem.Core.DTOs.Product
{
    public class CreateProductRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public string Unit { get; set; } = "Pcs";
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal MRP { get; set; }
        public decimal GstRate { get; set; }
        public decimal CessRate { get; set; } = 0;
        public bool IsGstInclusive { get; set; } = false;
        public decimal HsnCode { get; set; }
        public int InitialStock { get; set; } = 0;
        public int MinStockAlert { get; set; } = 5;
        public int CategoryId { get; set; }
        public int? SupplierId { get; set; }
    }

    public class UpdateProductRequest : CreateProductRequest
    {
        public int Id { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal MRP { get; set; }
        public decimal GstRate { get; set; }
        public decimal CessRate { get; set; }
        public bool IsGstInclusive { get; set; }
        public decimal HsnCode { get; set; }
        public int CurrentStock { get; set; }
        public int MinStockAlert { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string? SupplierName { get; set; }
        public int? SupplierId { get; set; }
        public bool IsLowStock => CurrentStock <= MinStockAlert;
    }

    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
    }

    public class CreateSupplierRequest
    {
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
    }

    public class SupplierDto : CreateSupplierRequest
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePurchaseOrderRequest
    {
        public string PurchaseBillNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public int SupplierId { get; set; }
        public string? Notes { get; set; }
        public List<PurchaseOrderItemRequest> Items { get; set; } = new();
    }

    public class PurchaseOrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal GstRate { get; set; }
        public decimal CessRate { get; set; } = 0;
    }

    public class PurchaseOrderDto
    {
        public int Id { get; set; }
        public string PurchaseBillNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal TotalGst { get; set; }
        public decimal TotalCess { get; set; }
        public decimal TotalAmount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<PurchaseOrderItemDto> Items { get; set; } = new();
    }

    public class PurchaseOrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string HsnCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal BuyingPrice { get; set; }
        public decimal GstRate { get; set; }
        public decimal GstAmount { get; set; }
        public decimal CessRate { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class StockSummaryDto
    {
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public List<ProductDto> LowStockProducts { get; set; } = new();
    }
}
