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
    public class SeminarParticipantController : ControllerBase
    {
        private readonly ISeminarParticipantService _service;

        public SeminarParticipantController(ISeminarParticipantService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách người tham dự các buổi Seminar do Lecturer tổ chức
        /// </summary>
        /// <returns>Danh sách người tham dự</returns>
        [HttpGet]
        [Authorize(Roles = "Lecturer")]
        public async Task<ActionResult<IEnumerable<SeminarParticipantResponse>>> GetAll()
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var items = await _service.GetAllForOrganizerAsync(organizerId);
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách các buổi Seminar mà người dùng hiện tại được mời tham dự
        /// </summary>
        /// <returns>Danh sách Seminar được mời</returns>
        [HttpGet("my-seminars")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SeminarInvitationResponse>>> GetMySeminars()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized();
            }

            var response = await _service.GetMyInvitationsAsync(currentUserId);
            return Ok(response);
        }

        /// <summary>
        /// Nộp form đánh giá / feedback cho buổi Seminar theo ID Seminar
        /// </summary>
        /// <param name="seminarId">ID buổi Seminar</param>
        /// <param name="request">Nội dung đánh giá</param>
        /// <returns>Kết quả nộp feedback</returns>
        [HttpPost("feedback/{seminarId:int}")]
        [HttpPost("{seminarId:int}/feedback")]
        [Authorize]
        public async Task<ActionResult<SeminarFeedbackResponse>> SubmitFeedback(int seminarId, [FromBody] SeminarFeedbackRequest request)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized();
            }

            if (request == null || string.IsNullOrWhiteSpace(request.ParticipantEvaluation))
            {
                return BadRequest(new { message = "Participant evaluation is required." });
            }

            try
            {
                var response = await _service.SubmitFeedbackAsync(seminarId, request, currentUserId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="seminarId">Lọc theo ID Seminar (tùy chọn)</param>
        /// <returns>Danh sách người tham dự có phân trang</returns>
        [HttpGet("paged")]
        [Authorize(Roles = "Lecturer")]
        public async Task<ActionResult<PagedResult<SeminarParticipantResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? seminarId = null)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var result = await _service.GetPagedForOrganizerAsync(paginationParams, organizerId, seminarId);
            return Ok(result);
        }

        /// <summary>
        /// Mời hoặc thêm người tham dự mới vào Seminar
        /// </summary>
        /// <param name="request">Thông tin người tham dự</param>
        /// <returns>Bản ghi người tham dự vừa tạo</returns>
        [HttpPost]
        [Authorize(Roles = "Lecturer")]
        public async Task<ActionResult<SeminarParticipantResponse>> Create([FromBody] SeminarParticipantCreateRequest request)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _service.CreateAsync(request, organizerId);
                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết người tham dự Seminar theo ID
        /// </summary>
        /// <param name="id">ID bản ghi người tham dự</param>
        /// <returns>Chi tiết người tham dự</returns>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Lecturer")]
        public async Task<ActionResult<SeminarParticipantResponse>> GetById(int id)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var item = await _service.GetByIdAsync(id, organizerId);
            if (item == null) return NotFound();

            return Ok(item);
        }

        /// <summary>
        /// Cập nhật trạng thái mời / đánh giá người tham dự Seminar
        /// </summary>
        /// <param name="id">ID bản ghi người tham dự</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Người tham dự sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Lecturer")]
        public async Task<ActionResult<SeminarParticipantResponse>> Update(int id, [FromBody] SeminarParticipantUpdateRequest request)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _service.UpdateAsync(id, request, organizerId);
                if (response == null) return NotFound();
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa người tham dự khỏi Seminar
        /// </summary>
        /// <param name="id">ID bản ghi người tham dự</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetCurrentUserId(out var organizerId))
            {
                return Unauthorized();
            }

            var success = await _service.DeleteAsync(id, organizerId);
            if (!success) return NotFound();

            return Ok(new { Message = "Deleted successfully." });
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out userId);
        }
    }
}