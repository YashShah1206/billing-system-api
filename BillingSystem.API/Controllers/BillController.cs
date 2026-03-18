using BillingSystem.Core.Common;
using BillingSystem.Core.DTOs.Bill;
using BillingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BillingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillController : ControllerBase
    {
        private readonly IBillService _billService;
        public BillController(IBillService billService) { _billService = billService; }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<BillDto>>> CreateBill([FromBody] CreateBillRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var bill = await _billService.CreateBillAsync(request, userId);
            return Ok(ApiResponse<BillDto>.Ok(bill, "Bill created successfully."));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<BillDto>>> GetBill(int id)
            => Ok(ApiResponse<BillDto>.Ok(await _billService.GetBillByIdAsync(id)));

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<BillListDto>>>> GetAllBills([FromQuery] BillFilterRequest filter)
        {
            // Non-admin users see only their own bills
            if (!User.IsInRole("Admin"))
                filter.CreatedByUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _billService.GetAllBillsAsync(filter);
            return Ok(ApiResponse<PagedResult<BillListDto>>.Ok(result));
        }

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DownloadBillPdf(int id)
        {
            var pdfBytes = await _billService.GenerateBillPdfAsync(id);
            var bill = await _billService.GetBillByIdAsync(id);
            return File(pdfBytes, "application/pdf", $"{bill.BillNumber}.pdf");
        }

        [HttpPost("{id}/save-pdf")]
        public async Task<ActionResult<ApiResponse<string>>> SaveBillPdf(int id)
        {
            var path = await _billService.SaveBillPdfAsync(id);
            return Ok(ApiResponse<string>.Ok(path, "PDF saved successfully."));
        }
    }
}
