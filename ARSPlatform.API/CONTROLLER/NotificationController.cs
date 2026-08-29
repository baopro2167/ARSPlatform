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
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        /// <summary>
        /// Lấy danh sách thông báo của người dùng hiện tại (trích xuất từ JWT token)
        /// </summary>
        /// <param name="userId">Chỉ Admin mới có quyền lọc theo ID người dùng khác</param>
        /// <returns>Danh sách thông báo</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetAll([FromQuery] int? userId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var targetUserId = (IsAdmin() && userId.HasValue) ? userId.Value : currentUserId.Value;
            var items = await _service.GetAllAsync(targetUserId);
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách thông báo có phân trang của người dùng hiện tại
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="userId">Chỉ Admin mới có quyền lọc theo ID người dùng khác</param>
        /// <returns>Danh sách thông báo có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<NotificationResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? userId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var targetUserId = (IsAdmin() && userId.HasValue) ? userId.Value : currentUserId.Value;
            var result = await _service.GetPagedAsync(paginationParams, targetUserId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy số lượng thông báo chưa đọc của người dùng hiện tại (dùng cho badge đỏ trên chuông)
        /// </summary>
        /// <returns>Số lượng thông báo chưa đọc</returns>
        [HttpGet("unread-count")]
        public async Task<ActionResult<UnreadNotificationCountResponse>> GetUnreadCount()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var count = await _service.GetUnreadCountAsync(currentUserId.Value);
            return Ok(new UnreadNotificationCountResponse { UnreadCount = count });
        }

        /// <summary>
        /// Lấy chi tiết thông báo theo ID (bảo mật: chỉ chủ sở hữu hoặc Admin)
        /// </summary>
        /// <param name="id">ID thông báo</param>
        /// <returns>Chi tiết thông báo</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<NotificationResponse>> GetById(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var item = await _service.GetByIdAsync(id, currentUserId.Value, IsAdmin());
                if (item == null) return NotFound(new { Message = "Notification not found." });
                return Ok(item);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc
        /// </summary>
        /// <returns>Số lượng thông báo đã cập nhật</returns>
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var count = await _service.MarkAllAsReadAsync(currentUserId.Value);
            return Ok(new
            {
                Message = "All notifications marked as read.",
                UpdatedCount = count
            });
        }

        /// <summary>
        /// Đánh dấu 1 thông báo cụ thể là đã đọc
        /// </summary>
        /// <param name="id">ID thông báo</param>
        /// <returns>Thông báo sau khi đánh dấu</returns>
        [HttpPut("{id:int}/read")]
        public async Task<ActionResult<NotificationResponse>> MarkAsRead(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var response = await _service.MarkAsReadAsync(id, currentUserId.Value, IsAdmin());
                if (response == null) return NotFound(new { Message = "Notification not found." });
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông báo
        /// </summary>
        /// <param name="id">ID thông báo</param>
        /// <param name="request">Dữ liệu cập nhật (VD: { "isRead": true })</param>
        /// <returns>Thông báo sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<NotificationResponse>> Update(int id, [FromBody] NotificationUpdateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var response = await _service.UpdateAsync(id, request, currentUserId.Value, IsAdmin());
                if (response == null) return NotFound(new { Message = "Notification not found." });
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa một thông báo (chỉ chủ sở hữu hoặc Admin mới có quyền xóa)
        /// </summary>
        /// <param name="id">ID thông báo cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var success = await _service.DeleteAsync(id, currentUserId.Value, IsAdmin());
                if (!success) return NotFound(new { Message = "Notification not found." });
                return Ok(new { Message = "Deleted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo mới một thông báo thủ công (Admin hoặc người gửi)
        /// </summary>
        /// <param name="request">Dữ liệu thông báo</param>
        /// <returns>Thông báo vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<NotificationResponse>> Create([FromBody] NotificationCreateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            // Nếu không truyền UserId đích thì mặc định là gửi cho chính mình
            if (!request.UserId.HasValue)
            {
                request.UserId = currentUserId.Value;
            }

            var response = await _service.CreateAsync(request);
            return Ok(response);
        }
    }
}
