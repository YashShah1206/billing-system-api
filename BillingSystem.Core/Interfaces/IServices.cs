using BillingSystem.Core.DTOs.Auth;
using BillingSystem.Core.DTOs.Bill;
using BillingSystem.Core.DTOs.Portfolio;
using BillingSystem.Core.DTOs.Product;
using BillingSystem.Core.DTOs.Tax;
using BillingSystem.Core.Entities;

namespace BillingSystem.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(int userId);
        Task UpdateUserStatusAsync(UpdateUserStatusRequest request, int adminId);
        Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    }

    public interface IProductService
    {
        Task<ProductDto> CreateProductAsync(CreateProductRequest request);
        Task<ProductDto> UpdateProductAsync(UpdateProductRequest request);
        Task DeleteProductAsync(int id);
        Task<ProductDto> GetProductByIdAsync(int id);
        Task<List<ProductDto>> GetAllProductsAsync();
        Task<StockSummaryDto> GetStockSummaryAsync();

        Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request);
        Task<List<CategoryDto>> GetAllCategoriesAsync();

        Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request);
        Task<List<SupplierDto>> GetAllSuppliersAsync();

        Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, int userId);
        Task<List<PurchaseOrderDto>> GetAllPurchaseOrdersAsync();
        Task<PurchaseOrderDto> GetPurchaseOrderByIdAsync(int id);
    }

    public interface IBillService
    {
        Task<BillDto> CreateBillAsync(CreateBillRequest request, int userId);
        Task<BillDto> GetBillByIdAsync(int id);
        Task<PagedResult<BillListDto>> GetAllBillsAsync(BillFilterRequest filter);
        Task<byte[]> GenerateBillPdfAsync(int billId);
        Task<string> SaveBillPdfAsync(int billId);
    }

    public interface IPortfolioService
    {
        Task<PortfolioSummaryDto> GetPortfolioAsync(PortfolioRequest request);
    }

    public interface ITaxService
    {
        Task<TaxReportDto> GetTaxReportAsync(TaxReportRequest request);
        Task<IncomeTaxSummaryDto> GetIncomeTaxSummaryAsync(int financialYear);
        Task<byte[]> ExportTaxReportAsync(TaxReportRequest request);
    }

    public interface IShopService
    {
        Task<Shop> GetShopDetailsAsync();
        Task<Shop> UpdateShopDetailsAsync(Shop shop);
    }

    public interface IJwtService
    {
        string GenerateToken(User user);
        int? ValidateToken(string token);
    }
}
