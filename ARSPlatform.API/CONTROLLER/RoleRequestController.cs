using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoleRequestController : ControllerBase
    {
        private readonly IRoleRequestService _roleRequestService;

        public RoleRequestController(
            IRoleRequestService roleRequestService)
        {
            _roleRequestService = roleRequestService;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ yêu cầu cấp vai trò (Role Requests) đang chờ Admin xử lý
        /// </summary>
        /// <returns>Danh sách yêu cầu cấp quyền</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(
            typeof(IEnumerable<RoleRequestResponse>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var items = await _roleRequestService.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách yêu cầu cấp quyền có phân trang dành cho Admin
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách yêu cầu cấp quyền có phân trang</returns>
        [HttpGet("paged")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(
            typeof(PagedResult<RoleRequestResponse>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<RoleRequestResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _roleRequestService.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết yêu cầu cấp vai trò theo ID
        /// </summary>
        /// <param name="id">ID yêu cầu cấp vai trò</param>
        /// <returns>Chi tiết yêu cầu</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _roleRequestService.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound(new
                {
                    Message = $"Role request {id} was not found."
                });
            }

            var currentUserId = GetCurrentUserId();
            if (!IsAdmin() && item.UserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(item);
        }

        /// <summary>
        /// Gửi yêu cầu xin cấp thêm vai trò mới
        /// </summary>
        /// <param name="request">Thông tin yêu cầu thêm vai trò</param>
        /// <returns>Chi tiết yêu cầu được tạo ở trạng thái PENDING</returns>
        [HttpPost]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAdditionalRole([FromBody] CreateAdditionalRoleRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            if (request == null)
            {
                return BadRequest(new { Message = "Request body is required." });
            }

            try
            {
                var result = await _roleRequestService.CreateAdditionalRoleAsync(
                    request,
                    currentUserId.Value,
                    IsAdmin());

                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy yêu cầu cấp vai trò đang ở trạng thái PENDING của người dùng hiện tại
        /// </summary>
        /// <param name="userId">ID người dùng (chỉ Admin mới có thể xem của người khác)</param>
        /// <returns>Yêu cầu đang chờ duyệt hoặc 204 No Content</returns>
        [HttpGet("my-pending")]
        [HttpGet("user/{userId:int}/pending")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyPending([FromRoute] int? userId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            int targetUserId;
            if (userId.HasValue && userId.Value > 0)
            {
                if (!IsAdmin() && userId.Value != currentUserId.Value)
                {
                    return Forbid();
                }
                targetUserId = userId.Value;
            }
            else
            {
                targetUserId = currentUserId.Value;
            }

            var item = await _roleRequestService.GetMyPendingAsync(targetUserId);
            if (item == null)
            {
                return NoContent();
            }

            return Ok(item);
        }

        /// <summary>
        /// Hủy yêu cầu cấp vai trò đang ở trạng thái PENDING
        /// </summary>
        /// <param name="requestId">ID yêu cầu cấp vai trò</param>
        /// <returns>204 No Content nếu hủy thành công</returns>
        [HttpDelete("{requestId:int}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelRequest(int requestId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                await _roleRequestService.CancelRequestAsync(
                    requestId,
                    currentUserId.Value,
                    IsAdmin());

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Admin duyệt hoặc từ chối yêu cầu cấp vai trò
        /// </summary>
        /// <param name="requestId">ID yêu cầu cấp vai trò</param>
        /// <param name="request">Quyết định phê duyệt (status = ACCEPTED hoặc REJECTED)</param>
        /// <returns>Kết quả duyệt yêu cầu</returns>
        [HttpPut("{requestId:int}/review")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Review(
            int requestId,
            [FromBody] RoleRequestReviewRequest request)
        {
            var adminId = GetCurrentUserId();
            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            if (request == null)
            {
                return BadRequest(new { Message = "Request body is required." });
            }

            try
            {
                var result = await _roleRequestService.ReviewAsync(
                    requestId,
                    adminId.Value,
                    request);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
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
        /// Phê duyệt yêu cầu cấp vai trò người dùng (Chuyển trạng thái Approved và gán Role)
        /// </summary>
        /// <param name="id">ID yêu cầu cấp vai trò</param>
        /// <param name="request">Ghi chú quyết định</param>
        /// <returns>Kết quả phê duyệt</returns>
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(
            int id,
            [FromBody] RoleRequestDecisionRequest? request)
        {
            var adminId = GetCurrentUserId();
            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            request ??= new RoleRequestDecisionRequest();

            try
            {
                var result = await _roleRequestService.ApproveAsync(
                    id,
                    adminId.Value,
                    request);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
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
        /// Từ chối yêu cầu cấp vai trò người dùng
        /// </summary>
        /// <param name="id">ID yêu cầu cấp vai trò</param>
        /// <param name="request">Lý do từ chối</param>
        /// <returns>Kết quả từ chối</returns>
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Deny(
            int id,
            [FromBody] RoleRequestDecisionRequest? request)
        {
            var adminId = GetCurrentUserId();
            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            request ??= new RoleRequestDecisionRequest();

            try
            {
                var result = await _roleRequestService.DenyAsync(
                    id,
                    adminId.Value,
                    request);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
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

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var userId) ? userId : null;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }
    }
}
