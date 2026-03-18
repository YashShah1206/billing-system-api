using BillingSystem.Core.Common;
using BillingSystem.Core.DTOs.Portfolio;
using BillingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        public PortfolioController(IPortfolioService portfolioService) { _portfolioService = portfolioService; }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PortfolioSummaryDto>>> GetPortfolio([FromQuery] PortfolioRequest request)
        {
            var result = await _portfolioService.GetPortfolioAsync(request);
            return Ok(ApiResponse<PortfolioSummaryDto>.Ok(result));
        }
    }
}
