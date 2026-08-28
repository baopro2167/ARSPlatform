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
    public class FollowerController : ControllerBase
    {
        private readonly IFollowerService _service;

        public FollowerController(IFollowerService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách theo dõi giữa các nhà nghiên cứu / người dùng
        /// </summary>
        /// <returns>Danh sách quan hệ theo dõi</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<FollowerResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách quan hệ theo dõi có phân trang</returns>
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<FollowerResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách người đang theo dõi một người dùng (Followers) có phân trang
        /// </summary>
        /// <param name="userId">ID người dùng được theo dõi</param>
        /// <param name="paginationParams">Tham số phân trang</param>
        /// <returns>Danh sách Followers</returns>
        [HttpGet("followers/{userId:int}/paged")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<FollowerResponse>>> GetFollowersPaged(
            int userId,
            [FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetByFollowedIdAsync(userId, paginationParams.PageNumber, paginationParams.PageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách người mà người dùng đang theo dõi (Following) có phân trang
        /// </summary>
        /// <param name="userId">ID người dùng đi theo dõi</param>
        /// <param name="paginationParams">Tham số phân trang</param>
        /// <returns>Danh sách Following</returns>
        [HttpGet("following/{userId:int}/paged")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<FollowerResponse>>> GetFollowingPaged(
            int userId,
            [FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetByFollowerIdAsync(userId, paginationParams.PageNumber, paginationParams.PageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thống kê số lượng Followers và Following của một người dùng
        /// </summary>
        /// <param name="userId">ID người dùng</param>
        /// <returns>Số lượng Followers và Following</returns>
        [HttpGet("counts/{userId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<FollowCountsResponse>> GetCounts(int userId)
        {
            var counts = await _service.GetCountsAsync(userId);
            return Ok(counts);
        }

        /// <summary>
        /// Kiểm tra người dùng hiện tại có đang theo dõi tác giả/người dùng này hay không
        /// </summary>
        /// <param name="followedId">ID tác giả/người dùng muốn kiểm tra</param>
        /// <returns>Trạng thái theo dõi (isFollowing)</returns>
        [HttpGet("is-following/{followedId:int}")]
        [Authorize]
        public async Task<ActionResult<object>> IsFollowing(int followedId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var isFollowing = await _service.IsFollowingAsync(currentUserId.Value, followedId);
            return Ok(new { FollowedId = followedId, IsFollowing = isFollowing });
        }

        /// <summary>
        /// Theo dõi một tác giả / người dùng
        /// </summary>
        /// <param name="request">Thông tin người cần theo dõi (FollowedId)</param>
        /// <returns>Bản ghi theo dõi vừa tạo</returns>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<FollowerResponse>> Create([FromBody] FollowerCreateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            try
            {
                var response = await _service.CreateAsync(request, currentUserId.Value);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Bật/Tắt theo dõi (Toggle Follow/Unfollow) một tác giả / người dùng
        /// </summary>
        /// <param name="followedId">ID người cần theo dõi hoặc bỏ theo dõi</param>
        /// <returns>Trạng thái mới sau khi toggle (isFollowing: true nếu vừa theo dõi, false nếu vừa bỏ theo dõi)</returns>
        [HttpPost("toggle/{followedId:int}")]
        [Authorize]
        public async Task<ActionResult<object>> ToggleFollow(int followedId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            try
            {
                var isNowFollowing = await _service.ToggleFollowAsync(currentUserId.Value, followedId);
                return Ok(new
                {
                    FollowedId = followedId,
                    IsFollowing = isNowFollowing,
                    Message = isNowFollowing ? "Successfully followed user." : "Successfully unfollowed user."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Hủy theo dõi (Unfollow) một tác giả / người dùng
        /// </summary>
        /// <param name="followedId">ID người muốn hủy theo dõi</param>
        /// <returns>Thông báo kết quả</returns>
        [HttpDelete("{followedId:int}")]
        [Authorize]
        public async Task<IActionResult> Unfollow(int followedId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var success = await _service.UnfollowAsync(currentUserId.Value, followedId);
            if (!success)
                return NotFound(new { Message = "Follow relationship not found." });

            return Ok(new { Message = "Successfully unfollowed user." });
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}
