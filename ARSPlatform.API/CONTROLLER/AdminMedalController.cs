using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/admin/medals")]
    [Authorize(Roles = "Admin")]
    public class AdminMedalController : ControllerBase
    {
        private readonly IMedalService _service;

        public AdminMedalController(IMedalService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách huy hiệu dạng nhóm theo tiêu chí (category) để phục vụ dropdown Admin,
        /// có hỗ trợ lọc theo Role (Researcher, Lecturer, Reviewer, Graduate Student, ALL).
        /// </summary>
        [HttpGet("dropdown")]
        [ProducesResponseType(typeof(IEnumerable<MedalDropdownCategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MedalDropdownCategoryDto>>> GetDropdown([FromQuery] string? role = null)
        {
            var categories = await _service.GetMedalsDropdownAsync(role);
            return Ok(categories);
        }

        /// <summary>
        /// Báo cáo thống kê tổng quan và chi tiết số lượng người dùng sở hữu từng huy hiệu.
        /// </summary>
        [HttpGet("analytics")]
        [ProducesResponseType(typeof(MedalAnalyticsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<MedalAnalyticsResponse>> GetAnalytics()
        {
            var analytics = await _service.GetMedalsAnalyticsAsync();
            return Ok(analytics);
        }

        /// <summary>
        /// Lấy danh sách chi tiết tất cả người dùng đang sở hữu huy hiệu được chỉ định (theo MedalId hoặc Code).
        /// </summary>
        [HttpGet("{id}/users")]
        [ProducesResponseType(typeof(IEnumerable<MedalUserDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<MedalUserDetailDto>>> GetMedalUsers(string id)
        {
            try
            {
                var users = await _service.GetMedalUsersAsync(id);
                return Ok(users);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tra cứu tiến độ thực tế theo thời gian thực của một User đối với một Medal cụ thể so với ngưỡng.
        /// </summary>
        [HttpGet("/api/admin/users/{userId:int}/medal-progress/{medalId}")]
        [ProducesResponseType(typeof(UserMedalProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserMedalProgressDto>> GetUserMedalProgress(int userId, string medalId)
        {
            try
            {
                var progress = await _service.GetUserMedalProgressAsync(userId, medalId);
                return Ok(progress);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
