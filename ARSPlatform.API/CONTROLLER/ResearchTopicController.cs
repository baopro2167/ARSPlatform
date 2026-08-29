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
    public class ResearchTopicController : ControllerBase
    {
        private readonly IResearchTopicService _service;

        public ResearchTopicController(IResearchTopicService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách đề tài nghiên cứu (có thể lọc theo lecturerId)
        /// </summary>
        /// <param name="lecturerId">ID Giảng viên phụ trách (tùy chọn)</param>
        /// <returns>Danh sách đề tài nghiên cứu</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ResearchTopicResponse>>> GetAll([FromQuery] int? lecturerId = null)
        {
            var items = await _service.GetAllAsync(lecturerId);
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách đề tài nghiên cứu của Giảng viên hiện tại
        /// </summary>
        /// <returns>Danh sách đề tài nghiên cứu của tôi</returns>
        [HttpGet("my-topics")]
        public async Task<ActionResult<IEnumerable<ResearchTopicResponse>>> GetMyTopics()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var items = await _service.GetMyTopicsAsync(currentUserId.Value);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="lecturerId">ID Giảng viên phụ trách (tùy chọn)</param>
        /// <returns>Danh sách đề tài nghiên cứu có phân trang</returns>
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ResearchTopicResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? lecturerId = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, lecturerId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo đề tài nghiên cứu mới (tự động gán LecturerId của tài khoản hiện tại)
        /// </summary>
        /// <param name="request">Thông tin đề tài nghiên cứu</param>
        /// <returns>Đề tài nghiên cứu vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<ResearchTopicResponse>> Create([FromBody] ResearchTopicCreateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var response = await _service.CreateAsync(request, currentUserId);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết đề tài nghiên cứu theo ID
        /// </summary>
        /// <param name="id">ID đề tài nghiên cứu</param>
        /// <returns>Chi tiết đề tài nghiên cứu</returns>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResearchTopicResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Research topic not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin đề tài nghiên cứu
        /// </summary>
        /// <param name="id">ID đề tài nghiên cứu</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Đề tài sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ResearchTopicResponse>> Update(int id, [FromBody] ResearchTopicUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Research topic not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một đề tài nghiên cứu
        /// </summary>
        /// <param name="id">ID đề tài nghiên cứu</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Research topic not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
