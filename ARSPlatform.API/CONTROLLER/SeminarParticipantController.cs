using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Lecturer")]
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
        /// Mời hoặc thêm người tham dự mới vào Seminar
        /// </summary>
        /// <param name="request">Thông tin người tham dự</param>
        /// <returns>Bản ghi người tham dự vừa tạo</returns>
        [HttpPost]
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