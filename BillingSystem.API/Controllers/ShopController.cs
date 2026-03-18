using BillingSystem.Core.Common;
using BillingSystem.Core.Entities;
using BillingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShopController : ControllerBase
    {
        private readonly IShopService _shopService;
        public ShopController(IShopService shopService) { _shopService = shopService; }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<Shop>>> GetShop()
            => Ok(ApiResponse<Shop>.Ok(await _shopService.GetShopDetailsAsync()));

        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Shop>>> UpdateShop([FromBody] Shop shop)
        {
            var result = await _shopService.UpdateShopDetailsAsync(shop);
            return Ok(ApiResponse<Shop>.Ok(result, "Shop details updated."));
        }
    }
}
