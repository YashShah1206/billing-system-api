using BillingSystem.Core.Common;
using BillingSystem.Core.DTOs.Tax;
using BillingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class TaxController : ControllerBase
    {
        private readonly ITaxService _taxService;
        public TaxController(ITaxService taxService) { _taxService = taxService; }

        [HttpGet("report")]
        public async Task<ActionResult<ApiResponse<TaxReportDto>>> GetTaxReport([FromQuery] TaxReportRequest request)
        {
            var result = await _taxService.GetTaxReportAsync(request);
            return Ok(ApiResponse<TaxReportDto>.Ok(result));
        }

        [HttpGet("report/export")]
        public async Task<IActionResult> ExportTaxReport([FromQuery] TaxReportRequest request)
        {
            var bytes = await _taxService.ExportTaxReportAsync(request);
            var fileName = $"TaxReport_{request.FromDate:yyyyMMdd}_to_{request.ToDate:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("income-tax/{financialYear}")]
        public async Task<ActionResult<ApiResponse<IncomeTaxSummaryDto>>> GetIncomeTax(int financialYear)
        {
            var result = await _taxService.GetIncomeTaxSummaryAsync(financialYear);
            return Ok(ApiResponse<IncomeTaxSummaryDto>.Ok(result));
        }
    }
}
