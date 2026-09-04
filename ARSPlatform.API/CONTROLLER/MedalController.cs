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
    public class MedalController : ControllerBase
    {
        private readonly IMedalService _service;

        public MedalController(IMedalService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ huy hiệu trên hệ thống (Hỗ trợ lọc theo role, tier, isActive, tìm kiếm).
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<MedalResponse>>> GetAll(
            [FromQuery] string? role = null,
            [FromQuery] string? tier = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null)
        {
            var items = await _service.GetAllAsync(role, tier, isActive, search);
            return Ok(items);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một huy hiệu theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<MedalResponse>> GetById(string id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { message = $"Medal with ID '{id}' not found." });
            }
            return Ok(item);
        }

        /// <summary>
        /// Admin tạo một huy hiệu mới.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MedalResponse>> Create([FromBody] MedalCreateRequest request)
        {
            try
            {
                var created = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin cập nhật thông tin huy hiệu (đổi ảnh imageUrl, tiêu đề, ngưỡng hoặc bật/tắt).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MedalResponse>> Update(string id, [FromBody] MedalUpdateRequest request)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, request);
                if (updated == null)
                {
                    return NotFound(new { message = $"Medal with ID '{id}' not found." });
                }
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin xóa huy hiệu khỏi hệ thống.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Medal with ID '{id}' not found." });
            }
            return Ok(new { message = $"Medal '{id}' deleted successfully." });
        }

        /// <summary>
        /// Admin khôi phục lại danh mục 26 huy hiệu chuẩn mặc định.
        /// </summary>
        [HttpPost("reset-defaults")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<MedalResponse>>> ResetToDefaults()
        {
            var result = await _service.ResetToDefaultsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách toàn bộ huy hiệu của tài khoản đang đăng nhập (gồm huy hiệu đã đạt và đang tích lũy).
        /// </summary>
        [HttpGet("my-medals")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserMedalResponse>>> GetMyMedals()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var medals = await _service.GetMyMedalsAsync(userId.Value);
            return Ok(medals);
        }

        /// <summary>
        /// Lấy danh sách các huy hiệu đã mở khóa công khai của một người dùng bất kỳ (dùng cho Profile và Forum).
        /// </summary>
        [HttpGet("user/{userId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<UserMedalResponse>>> GetUserUnlockedMedals(int userId)
        {
            var medals = await _service.GetUserUnlockedMedalsAsync(userId);
            return Ok(medals);
        }
    }
}
