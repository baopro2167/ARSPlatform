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
    [Route("api/LearningMaterialShare")]
    [Authorize]
    public class SharedMaterialController : ControllerBase
    {
        private readonly ISharedMaterialService _service;

        public SharedMaterialController(ISharedMaterialService service)
        {
            _service = service;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : 0;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        /// <summary>
        /// Lấy danh sách tài liệu chia sẻ của người dùng hiện tại (bao gồm tài liệu tôi gửi hoặc nhận).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SharedMaterialResponse>>> GetAll(
            [FromQuery] string? role = null,
            [FromQuery] bool includeExpired = false,
            [FromQuery] string? status = null,
            [FromQuery] int? learningMaterialId = null)
        {
            var userId = GetCurrentUserId();
            var items = await _service.GetFeedAsync(userId, includeExpired, status, learningMaterialId);
            return Ok(items);
        }

        /// <summary>
        /// Phân trang danh sách chia sẻ
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<SharedMaterialResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Tạo lời mời chia sẻ tài liệu học tập với đồng nghiệp (thời hạn 30 ngày)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SharedMaterialResponse>> Create([FromBody] SharedMaterialCreateRequest request)
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            try
            {
                var response = await _service.CreateShareAsync(request, userId, isAdmin);
                return StatusCode(201, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết bản ghi chia sẻ
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SharedMaterialResponse>> GetById(int id)
        {
            var userId = GetCurrentUserId();
            var item = await _service.GetByIdAsync(id, userId);
            if (item == null) return NotFound(new { Message = "Shared material not found." });
            return Ok(item);
        }

        /// <summary>
        /// Phản hồi lời mời chia sẻ (Accept / Decline bởi người nhận) hoặc chỉnh sửa
        /// </summary>
        [HttpPatch("{id:int}")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<SharedMaterialResponse>> Update(int id, [FromBody] SharedMaterialUpdateRequest request)
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            try
            {
                var response = await _service.UpdateAsync(id, request, userId, isAdmin);
                if (response == null) return NotFound(new { Message = "Shared material not found." });
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Thu hồi hoặc xóa lời mời chia sẻ tài liệu (Dành cho người gửi hoặc Admin)
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            try
            {
                var success = await _service.RevokeOrDeleteAsync(id, userId, isAdmin);
                if (!success) return NotFound(new { Message = "Shared material not found." });
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }
}
