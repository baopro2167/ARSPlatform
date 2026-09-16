using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedalController : ControllerBase
    {
        private readonly IMedalService _service;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public MedalController(
            IMedalService service,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _service = service;
            _env = env;
            _config = config;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        private string GetCurrentUserName()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                ?? User.Identity?.Name
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? "Admin";
        }

        private bool IsDevGated()
        {
            var isProd = _env.IsProduction();
            var allowDevGrants = _config.GetValue<bool>("Medal:AllowDevGrants");
            return isProd && !allowDevGrants;
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
        /// Admin xóa huy hiệu khỏi hệ thống. Nếu có người dùng đang giữ huy hiệu này thì trả về 409 Conflict.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = $"Medal with ID '{id}' not found." });
                }
                return Ok(new { message = $"Medal '{id}' deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin bật/tắt trạng thái hiển thị của một bản ghi UserMedal (huy hiệu đã cấp cho user cụ thể).
        /// Body: { "status": "Active" | "Inactive" }.
        /// </summary>
        /// <param name="userMedalId">ID bản ghi UserMedal cần đổi trạng thái.</param>
        /// <param name="request">Body chứa trạng thái mới.</param>
        [HttpPatch("admin/user-medal/{userMedalId:long}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UserMedalResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserMedalResponse>> UpdateUserMedalStatus(
            long userMedalId,
            [FromBody] MedalUserMedalStatusUpdateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { message = "Status is required." });
            }

            if (!Enum.TryParse<MedalStatus>(request.Status, ignoreCase: true, out var newStatus)
                || !Enum.IsDefined(typeof(MedalStatus), newStatus))
            {
                return BadRequest(new { message = "Invalid status. Allowed values: 'Active' or 'Inactive'." });
            }

            var adminId = GetCurrentUserId();
            if (!adminId.HasValue)
            {
                return Unauthorized(new { message = "Admin is not authenticated." });
            }
            var adminName = GetCurrentUserName();

            try
            {
                var updated = await _service.UpdateUserMedalStatusAsync(userMedalId, newStatus, adminId.Value, adminName);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
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
        /// Lấy danh sách huy hiệu của người dùng. Mặc định trả về huy hiệu đã mở khóa. Nếu includeLocked=true, trả về toàn bộ tiến trình huy hiệu (yêu cầu quyền Admin, chính chủ, hoặc Giảng viên hướng dẫn).
        /// </summary>
        /// <param name="userId">ID của người dùng cần lấy danh sách huy hiệu.</param>
        /// <param name="includeLocked">Khi true, trả về cả huy hiệu đã khóa và đang tích lũy.</param>
        [HttpGet("user/{userId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<UserMedalResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<UserMedalResponse>>> GetUserMedals(
            int userId,
            [FromQuery] bool includeLocked = false)
        {
            try
            {
                var callerId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");
                var medals = await _service.GetUserMedalsAsync(userId, includeLocked, callerId, isAdmin);
                return Ok(medals);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                if (!GetCurrentUserId().HasValue)
                {
                    return Unauthorized(new { message = "Authentication required to view locked medals." });
                }
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin cấp hoặc cập nhật tiến trình huy hiệu cho người dùng theo cách thủ công. Idempotent per (userId, medalCode).
        /// </summary>
        /// <param name="request">Thông tin cấp huy hiệu (userId, medalCode, forceUnlocked, awardedReason).</param>
        [HttpPost("grant")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UserMedalResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(UserMedalResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserMedalResponse>> GrantMedal([FromBody] MedalGrantRequest request)
        {
            var adminId = GetCurrentUserId();
            if (!adminId.HasValue)
            {
                return Unauthorized(new { message = "Admin is not authenticated." });
            }
            var adminName = GetCurrentUserName();

            try
            {
                var (response, isCreated) = await _service.GrantMedalAsync(request, adminId.Value, adminName);
                if (isCreated)
                {
                    return StatusCode(StatusCodes.Status201Created, response);
                }
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// [Dev/Staging Helper] Cấp toàn bộ huy hiệu phù hợp với vai trò của người dùng trong một transaction duy nhất. Bị chặn trên môi trường Production (trả về 404).
        /// </summary>
        /// <param name="request">Tham số cấp toàn bộ huy hiệu theo vai trò (userId, includePlatinum, tierFilter, awardedReason).</param>
        [HttpPost("dev/grant-all-by-role")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(MedalDevGrantAllResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MedalDevGrantAllResponse>> DevGrantAllByRole([FromBody] MedalDevGrantAllRequest request)
        {
            if (IsDevGated())
            {
                return NotFound(new { message = "Endpoint not available in production." });
            }

            var adminId = GetCurrentUserId();
            if (!adminId.HasValue)
            {
                return Unauthorized(new { message = "Admin is not authenticated." });
            }
            var adminName = GetCurrentUserName();

            try
            {
                var result = await _service.DevGrantAllByRoleAsync(request, adminId.Value, adminName);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
