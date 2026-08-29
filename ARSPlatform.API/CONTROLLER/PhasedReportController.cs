using System;
using System.Collections.Generic;
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
    [Authorize]
    public class PhasedReportController : ControllerBase
    {
        private readonly IPhasedReportService _service;

        public PhasedReportController(IPhasedReportService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách báo cáo tiến độ (có thể lọc theo researchGroupId)
        /// </summary>
        /// <param name="researchGroupId">ID nhóm nghiên cứu (tùy chọn)</param>
        /// <returns>Danh sách báo cáo tiến độ</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhasedReportResponse>>> GetAll([FromQuery] int? researchGroupId = null)
        {
            var items = await _service.GetAllAsync(researchGroupId);
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách báo cáo tiến độ theo nhóm nghiên cứu
        /// </summary>
        /// <param name="groupId">ID nhóm nghiên cứu</param>
        /// <returns>Danh sách báo cáo</returns>
        [HttpGet("group/{groupId:int}")]
        public async Task<ActionResult<IEnumerable<PhasedReportResponse>>> GetByGroup(int groupId)
        {
            var items = await _service.GetAllAsync(groupId);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="researchGroupId">ID nhóm nghiên cứu (tùy chọn)</param>
        /// <returns>Danh sách báo cáo tiến độ có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<PhasedReportResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? researchGroupId = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, researchGroupId);
            return Ok(result);
        }

        /// <summary>
        /// Sinh viên nộp báo cáo tiến độ theo giai đoạn (Phase 1 -> Phase 4)
        /// </summary>
        /// <param name="request">Thông tin báo cáo nộp</param>
        /// <returns>Báo cáo vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<PhasedReportResponse>> Create([FromBody] PhasedReportCreateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var response = await _service.CreateAsync(request, currentUserId);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết báo cáo tiến độ theo ID
        /// </summary>
        /// <param name="id">ID báo cáo</param>
        /// <returns>Chi tiết báo cáo tiến độ</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PhasedReportResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Phased report not found." });
            return Ok(item);
        }

        /// <summary>
        /// Giảng viên chấm điểm &amp; đánh giá báo cáo tiến độ (Pass/Reject, điểm số và nhận xét)
        /// </summary>
        /// <param name="id">ID báo cáo cần chấm điểm / cập nhật</param>
        /// <param name="request">Dữ liệu đánh giá</param>
        /// <returns>Báo cáo sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<PhasedReportResponse>> Update(int id, [FromBody] PhasedReportUpdateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var response = await _service.UpdateAsync(id, request, currentUserId);
            if (response == null) return NotFound(new { Message = "Phased report not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một báo cáo tiến độ
        /// </summary>
        /// <param name="id">ID báo cáo cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Phased report not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
