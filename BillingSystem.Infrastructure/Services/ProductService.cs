using BillingSystem.Core.DTOs.Product;
using BillingSystem.Core.Entities;
using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---- PRODUCT ----
        public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
        {
            if (await _context.Products.AnyAsync(p => p.ProductCode == request.ProductCode))
                throw new InvalidOperationException($"Product code '{request.ProductCode}' already exists.");

            var product = new Product
            {
                ProductName = request.ProductName,
                ProductCode = request.ProductCode.ToUpper().Trim(),
                Barcode = request.Barcode,
                Description = request.Description,
                Unit = request.Unit,
                BuyingPrice = request.BuyingPrice,
                SellingPrice = request.SellingPrice,
                MRP = request.MRP,
                GstRate = request.GstRate,
                CessRate = request.CessRate,
                IsGstInclusive = request.IsGstInclusive,
                HsnCode = request.HsnCode,
                CurrentStock = request.InitialStock,
                MinStockAlert = request.MinStockAlert,
                CategoryId = request.CategoryId,
                SupplierId = request.SupplierId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return await GetProductByIdAsync(product.Id);
        }

        public async Task<ProductDto> UpdateProductAsync(UpdateProductRequest request)
        {
            var product = await _context.Products.FindAsync(request.Id)
                ?? throw new KeyNotFoundException("Product not found.");

            product.ProductName = request.ProductName;
            product.Barcode = request.Barcode;
            product.Description = request.Description;
            product.Unit = request.Unit;
            product.BuyingPrice = request.BuyingPrice;
            product.SellingPrice = request.SellingPrice;
            product.MRP = request.MRP;
            product.GstRate = request.GstRate;
            product.CessRate = request.CessRate;
            product.IsGstInclusive = request.IsGstInclusive;
            product.HsnCode = request.HsnCode;
            product.MinStockAlert = request.MinStockAlert;
            product.CategoryId = request.CategoryId;
            product.SupplierId = request.SupplierId;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetProductByIdAsync(product.Id);
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id)
                ?? throw new KeyNotFoundException("Product not found.");
            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException("Product not found.");
            return MapProductToDto(product);
        }

        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
            return products.Select(MapProductToDto).ToList();
        }

        public async Task<StockSummaryDto> GetStockSummaryAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .ToListAsync();

            var lowStock = products.Where(p => p.CurrentStock > 0 && p.CurrentStock <= p.MinStockAlert).ToList();
            var outOfStock = products.Where(p => p.CurrentStock == 0).ToList();

            return new StockSummaryDto
            {
                TotalProducts = products.Count,
                LowStockCount = lowStock.Count,
                OutOfStockCount = outOfStock.Count,
                TotalInventoryValue = products.Sum(p => p.CurrentStock * p.BuyingPrice),
                LowStockProducts = lowStock.Select(MapProductToDto).ToList()
            };
        }

        // ---- CATEGORY ----
        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
        {
            if (await _context.Categories.AnyAsync(c => c.Name == request.Name))
                throw new InvalidOperationException("Category already exists.");

            var cat = new Category { Name = request.Name, Description = request.Description };
            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();
            return new CategoryDto { Id = cat.Id, Name = cat.Name, Description = cat.Description };
        }

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductCount = c.Products.Count(p => !p.IsDeleted)
                }).ToListAsync();
        }

        // ---- SUPPLIER ----
        public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request)
        {
            var supplier = new Supplier
            {
                SupplierName = request.SupplierName,
                ContactPerson = request.ContactPerson,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Address = request.Address,
                City = request.City,
                State = request.State,
                GstNumber = request.GstNumber
            };
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return new SupplierDto
            {
                Id = supplier.Id,
                SupplierName = supplier.SupplierName,
                ContactPerson = supplier.ContactPerson,
                PhoneNumber = supplier.PhoneNumber,
                Email = supplier.Email,
                Address = supplier.Address,
                City = supplier.City,
                State = supplier.State,
                GstNumber = supplier.GstNumber,
                CreatedAt = supplier.CreatedAt
            };
        }

        public async Task<List<SupplierDto>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers
                .Where(s => !s.IsDeleted)
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    SupplierName = s.SupplierName,
                    ContactPerson = s.ContactPerson,
                    PhoneNumber = s.PhoneNumber,
                    Email = s.Email,
                    Address = s.Address,
                    City = s.City,
                    State = s.State,
                    GstNumber = s.GstNumber,
                    CreatedAt = s.CreatedAt
                }).ToListAsync();
        }

        // ---- PURCHASE ORDER ----
        public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, int userId)
        {
            var order = new PurchaseOrder
            {
                PurchaseBillNumber = request.PurchaseBillNumber,
                PurchaseDate = request.PurchaseDate,
                SupplierId = request.SupplierId,
                Notes = request.Notes,
                CreatedByUserId = userId
            };

            decimal subTotal = 0, totalGst = 0, totalCess = 0;

            foreach (var itemReq in request.Items)
            {
                var product = await _context.Products.FindAsync(itemReq.ProductId)
                    ?? throw new KeyNotFoundException($"Product {itemReq.ProductId} not found.");

                var gstAmt = itemReq.BuyingPrice * itemReq.Quantity * itemReq.GstRate / 100;
                var cessAmt = itemReq.BuyingPrice * itemReq.Quantity * itemReq.CessRate / 100;
                var lineTotal = (itemReq.BuyingPrice * itemReq.Quantity) + gstAmt + cessAmt;

                order.Items.Add(new PurchaseOrderItem
                {
                    ProductId = itemReq.ProductId,
                    Quantity = itemReq.Quantity,
                    BuyingPrice = itemReq.BuyingPrice,
                    GstRate = itemReq.GstRate,
                    GstAmount = gstAmt,
                    CessRate = itemReq.CessRate,
                    CessAmount = cessAmt,
                    TotalAmount = lineTotal
                });

                // Update stock and buying price
                product.CurrentStock += itemReq.Quantity;
                product.BuyingPrice = itemReq.BuyingPrice;
                product.UpdatedAt = DateTime.UtcNow;

                subTotal += itemReq.BuyingPrice * itemReq.Quantity;
                totalGst += gstAmt;
                totalCess += cessAmt;
            }

            order.SubTotal = subTotal;
            order.TotalGst = totalGst;
            order.TotalCess = totalCess;
            order.TotalAmount = subTotal + totalGst + totalCess;

            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();
            return await GetPurchaseOrderByIdAsync(order.Id);
        }

        public async Task<List<PurchaseOrderDto>> GetAllPurchaseOrdersAsync()
        {
            var orders = await _context.PurchaseOrders
                .Include(o => o.Supplier)
                .Include(o => o.CreatedByUser)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .OrderByDescending(o => o.PurchaseDate)
                .ToListAsync();
            return orders.Select(MapPurchaseOrderToDto).ToList();
        }

        public async Task<PurchaseOrderDto> GetPurchaseOrderByIdAsync(int id)
        {
            var order = await _context.PurchaseOrders
                .Include(o => o.Supplier)
                .Include(o => o.CreatedByUser)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new KeyNotFoundException("Purchase order not found.");
            return MapPurchaseOrderToDto(order);
        }

        // ---- MAPPERS ----
        private static ProductDto MapProductToDto(Product p) => new()
        {
            Id = p.Id,
            ProductName = p.ProductName,
            ProductCode = p.ProductCode,
            Barcode = p.Barcode,
            Description = p.Description,
            Unit = p.Unit,
            BuyingPrice = p.BuyingPrice,
            SellingPrice = p.SellingPrice,
            MRP = p.MRP,
            GstRate = p.GstRate,
            CessRate = p.CessRate,
            IsGstInclusive = p.IsGstInclusive,
            HsnCode = p.HsnCode,
            CurrentStock = p.CurrentStock,
            MinStockAlert = p.MinStockAlert,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? "",
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier?.SupplierName
        };

        private static PurchaseOrderDto MapPurchaseOrderToDto(PurchaseOrder o) => new()
        {
            Id = o.Id,
            PurchaseBillNumber = o.PurchaseBillNumber,
            PurchaseDate = o.PurchaseDate,
            SupplierName = o.Supplier?.SupplierName ?? "",
            SubTotal = o.SubTotal,
            TotalGst = o.TotalGst,
            TotalCess = o.TotalCess,
            TotalAmount = o.TotalAmount,
            CreatedBy = o.CreatedByUser?.FullName ?? "",
            CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new PurchaseOrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.ProductName ?? "",
                HsnCode = i.Product?.HsnCode.ToString() ?? "",
                Quantity = i.Quantity,
                Unit = i.Product?.Unit ?? "",
                BuyingPrice = i.BuyingPrice,
                GstRate = i.GstRate,
                GstAmount = i.GstAmount,
                CessRate = i.CessRate,
                CessAmount = i.CessAmount,
                TotalAmount = i.TotalAmount
            }).ToList()
        };
    }
}
