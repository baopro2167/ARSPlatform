using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _service;

        public AuditLogController(IAuditLogService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách nhật ký kiểm toán (Audit Logs) phân trang và lọc theo thời gian/admin
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm</param>
        /// <param name="adminId">Lọc theo Admin ID</param>
        /// <param name="range">Khoảng thời gian: all_time, today, 7_days, 30_days</param>
        /// <param name="paginationParams">Tham số phân trang</param>
        /// <returns>Danh sách Audit Log phân trang</returns>
        [HttpGet]
        public async Task<ActionResult<PagedResult<AuditLogResponse>>> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] int? adminId = null,
            [FromQuery] string? range = "all_time",
            [FromQuery] PaginationParams? paginationParams = null)
        {
            try
            {
                var result = await _service.GetPagedAsync(search, adminId, range, paginationParams);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xuất toàn bộ nhật ký kiểm toán ra file CSV
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm</param>
        /// <param name="adminId">Lọc theo Admin ID</param>
        /// <param name="range">Khoảng thời gian</param>
        /// <returns>File CSV nhật ký kiểm toán</returns>
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] string? search = null,
            [FromQuery] int? adminId = null,
            [FromQuery] string? range = "all_time")
        {
            try
            {
                var bytes = await _service.ExportCsvAsync(search, adminId, range);
                return File(bytes, "text/csv; charset=utf-8", $"audit-log-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Ghi mới một bản ghi nhật ký kiểm toán
        /// </summary>
        /// <param name="request">Thông tin bản ghi kiểm toán</param>
        /// <returns>Bản ghi kiểm toán vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<AuditLogResponse>> Create([FromBody] AuditLogCreateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { Message = "Request body is required." });
            }

            var currentAdminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentAdminIdClaim, out var currentAdminId) && currentAdminId != request.AdminId)
            {
                return Forbid();
            }

            try
            {
                var response = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetAll), new { }, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}