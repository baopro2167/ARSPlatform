using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LecturerController : ControllerBase
    {
        private readonly IUserService _userService;

        public LecturerController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Lấy danh sách giảng viên đang hoạt động (loại trừ tài khoản hiện tại)
        /// </summary>
        [HttpGet]
        [HttpGet("roster")]
        public async Task<ActionResult<List<UserResponse>>> GetLecturers([FromQuery] PaginationParams? paginationParams = null)
        {
            int? currentUserId = null;
            if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid))
            {
                currentUserId = uid;
            }

            var roster = await _userService.GetLecturersRosterAsync(excludeUserId: currentUserId);
            return Ok(roster);
        }
    }
}
