using BillingSystem.Core.Common;
using BillingSystem.Core.DTOs.Product;
using BillingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BillingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService) { _productService = productService; }

        // Products
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetAll()
            => Ok(ApiResponse<List<ProductDto>>.Ok(await _productService.GetAllProductsAsync()));

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(int id)
            => Ok(ApiResponse<ProductDto>.Ok(await _productService.GetProductByIdAsync(id)));

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Create([FromBody] CreateProductRequest request)
        {
            var result = await _productService.CreateProductAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ProductDto>.Ok(result, "Product created successfully."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Update(int id, [FromBody] UpdateProductRequest request)
        {
            request.Id = id;
            var result = await _productService.UpdateProductAsync(request);
            return Ok(ApiResponse<ProductDto>.Ok(result, "Product updated successfully."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            return Ok(ApiResponse.Ok("Product deleted successfully."));
        }

        [HttpGet("stock-summary")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<StockSummaryDto>>> GetStockSummary()
            => Ok(ApiResponse<StockSummaryDto>.Ok(await _productService.GetStockSummaryAsync()));

        // Categories
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
            => Ok(ApiResponse<List<CategoryDto>>.Ok(await _productService.GetAllCategoriesAsync()));

        [HttpPost("categories")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            var result = await _productService.CreateCategoryAsync(request);
            return Ok(ApiResponse<CategoryDto>.Ok(result, "Category created."));
        }

        // Suppliers
        [HttpGet("suppliers")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<SupplierDto>>>> GetSuppliers()
            => Ok(ApiResponse<List<SupplierDto>>.Ok(await _productService.GetAllSuppliersAsync()));

        [HttpPost("suppliers")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier([FromBody] CreateSupplierRequest request)
        {
            var result = await _productService.CreateSupplierAsync(request);
            return Ok(ApiResponse<SupplierDto>.Ok(result, "Supplier added."));
        }

        // Purchase Orders
        [HttpGet("purchases")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<PurchaseOrderDto>>>> GetPurchases()
            => Ok(ApiResponse<List<PurchaseOrderDto>>.Ok(await _productService.GetAllPurchaseOrdersAsync()));

        [HttpGet("purchases/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> GetPurchase(int id)
            => Ok(ApiResponse<PurchaseOrderDto>.Ok(await _productService.GetPurchaseOrderByIdAsync(id)));

        [HttpPost("purchases")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CreatePurchase([FromBody] CreatePurchaseOrderRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _productService.CreatePurchaseOrderAsync(request, userId);
            return Ok(ApiResponse<PurchaseOrderDto>.Ok(result, "Purchase order created and stock updated."));
        }
    }
}
