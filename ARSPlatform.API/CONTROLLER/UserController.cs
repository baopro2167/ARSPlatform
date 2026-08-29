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
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Lấy danh sách người dùng phân trang (Dành cho Admin)
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách người dùng</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResult<UserResponse>>> GetUsers([FromQuery] PaginationParams paginationParams)
        {
            var result = await _userService.GetUsersAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách người dùng có phân trang</returns>
        [HttpGet("paged")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResult<UserResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _userService.GetUsersAsync(paginationParams);
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
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole != "Admin" && currentUserIdStr != id.ToString())
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
