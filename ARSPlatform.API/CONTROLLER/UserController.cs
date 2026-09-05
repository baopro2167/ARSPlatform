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
    [Route("api/Account")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Lấy danh sách người dùng phân trang (Admin hoặc người dùng tìm kiếm đồng nghiệp)
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="role">Lọc theo vai trò (Lecturer, Researcher, v.v.)</param>
        /// <param name="isActive">Lọc theo trạng thái kích hoạt</param>
        /// <returns>Danh sách người dùng</returns>
        [HttpGet]
        public async Task<ActionResult<PagedResult<UserResponse>>> GetUsers(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null)
        {
            var isAdmin = User.IsInRole("Admin");
            int? currentUserId = null;
            if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid))
            {
                currentUserId = uid;
            }

            if (!isAdmin)
            {
                // Non-admins can only view active users, defaulting role to Lecturer if not provided, and excluding themselves
                isActive = true;
                if (string.IsNullOrWhiteSpace(role))
                {
                    role = "Lecturer";
                }
                var result = await _userService.GetUsersAsync(paginationParams, role, isActive, excludeUserId: currentUserId);
                return Ok(result);
            }
            else
            {
                var result = await _userService.GetUsersAsync(paginationParams, role, isActive, excludeUserId: null);
                return Ok(result);
            }
        }

        /// <summary>
        /// Lấy danh sách người dùng có phân trang
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<UserResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null)
        {
            return await GetUsers(paginationParams, role, isActive);
        }

        /// <summary>
        /// Lấy danh sách giảng viên để chia sẻ tài liệu
        /// </summary>
        [HttpGet("lecturers")]
        public async Task<ActionResult<System.Collections.Generic.List<UserResponse>>> GetLecturers()
        {
            int? currentUserId = null;
            if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid))
            {
                currentUserId = uid;
            }
            var result = await _userService.GetLecturersRosterAsync(excludeUserId: currentUserId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết thông tin người dùng theo ID (Công khai cho người dùng đã đăng nhập)
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <returns>Thông tin chi tiết người dùng</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponse>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            return Ok(user);
        }

        /// <summary>
        /// Cập nhật thông tin người dùng (Họ tên, ảnh đại diện, trạng thái)
        /// </summary>
        /// <param name="id">ID người dùng cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Thông tin người dùng sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserResponse>> UpdateUser(int id, [FromBody] UserUpdateRequest request)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!User.IsInRole("Admin") && currentUserIdStr != id.ToString())
            {
                return Forbid();
            }

            try
            {
                var updatedUser = await _userService.UpdateUserAsync(id, request);
                if (updatedUser == null)
                    return NotFound();

                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Khóa tài khoản người dùng (Dành cho Admin)
        /// </summary>
        [HttpPost("{id:int}/suspend")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserResponse>> SuspendUser(int id)
        {
            var updated = await _userService.UpdateUserAsync(id, new UserUpdateRequest { IsActive = false });
            if (updated == null)
                return NotFound(new { Message = "User not found." });

            return Ok(updated);
        }

        /// <summary>
        /// Mở khóa tài khoản người dùng (Dành cho Admin)
        /// </summary>
        [HttpPost("{id:int}/unsuspend")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserResponse>> UnsuspendUser(int id)
        {
            var updated = await _userService.UpdateUserAsync(id, new UserUpdateRequest { IsActive = true });
            if (updated == null)
                return NotFound(new { Message = "User not found." });

            return Ok(updated);
        }

        /// <summary>
        /// Xóa tài khoản người dùng khỏi hệ thống (Chỉ dành cho Admin)
        /// </summary>
        /// <param name="id">ID người dùng cần xóa</param>
        /// <returns>Thông báo kết quả</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
                return NotFound();

            return Ok(new { Message = "User deleted successfully." });
        }
    }
}
