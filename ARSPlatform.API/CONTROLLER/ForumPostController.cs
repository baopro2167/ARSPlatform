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
    public class ForumPostController : ControllerBase
    {
        private readonly IForumPostService _service;

        public ForumPostController(IForumPostService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy danh sách tất cả các bài viết trên diễn đàn (có hỗ trợ tìm kiếm, lọc theo chủ đề, sắp xếp và trạng thái isLiked)
        /// </summary>
        /// <param name="category">Chuyên mục / Lĩnh vực lọc</param>
        /// <param name="sort">Kiểu sắp xếp: popular, newest</param>
        /// <param name="search">Từ khóa tìm kiếm trong tiêu đề hoặc nội dung</param>
        /// <returns>Danh sách bài viết diễn đàn</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ForumPostResponse>>> GetAll(
            [FromQuery] string? category,
            [FromQuery] string? sort,
            [FromQuery] string? search)
        {
            var currentUserId = GetCurrentUserId();
            var items = await _service.GetAllAsync(category, sort, search, currentUserId);
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách bài viết có phân trang (kèm trạng thái isLiked của user hiện tại)
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="category">Chuyên mục / Lĩnh vực lọc</param>
        /// <param name="sort">Kiểu sắp xếp: popular, newest</param>
        /// <param name="search">Từ khóa tìm kiếm trong tiêu đề hoặc nội dung</param>
        /// <returns>Danh sách bài viết diễn đàn có phân trang</returns>
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ForumPostResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] string? category = null,
            [FromQuery] string? sort = null,
            [FromQuery] string? search = null)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _service.GetPagedAsync(paginationParams, category, sort, search, currentUserId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách ID các bài viết mà người dùng hiện tại đã like
        /// </summary>
        /// <returns>Mảng số nguyên ID bài viết [1, 4, 14]</returns>
        [HttpGet("my-likes")]
        [Authorize]
        public async Task<ActionResult<List<int>>> GetMyLikes()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var likedIds = await _service.GetMyLikedPostIdsAsync(currentUserId.Value);
            return Ok(likedIds);
        }

        /// <summary>
        /// Lấy chi tiết một bài viết trên diễn đàn theo ID (kèm trạng thái isLiked)
        /// </summary>
        /// <param name="id">ID bài viết</param>
        /// <returns>Thông tin chi tiết bài viết và danh sách bình luận</returns>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ForumPostResponse>> GetById(int id)
        {
            var currentUserId = GetCurrentUserId();
            var item = await _service.GetByIdAsync(id, currentUserId);
            if (item == null) return NotFound(new { Message = "Forum post not found." });
            return Ok(item);
        }

        /// <summary>
        /// Bật/Tắt thích bài viết (Toggle Like / Unlike)
        /// </summary>
        /// <param name="id">ID bài viết cần like hoặc bỏ like</param>
        /// <returns>Trạng thái like mới và tổng số lượt like</returns>
        [HttpPost("{id:int}/like")]
        [HttpPost("like/{id:int}")]
        [Authorize]
        public async Task<ActionResult<ForumPostLikeToggleResponse>> ToggleLike(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { Message = "You must be logged in to like a post." });
            }

            try
            {
                var response = await _service.ToggleLikeAsync(id, currentUserId.Value);
                return Ok(response);
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
        /// Tạo mới một bài viết trên diễn đàn
        /// </summary>
        /// <param name="request">Thông tin bài viết cần tạo</param>
        /// <returns>Bài viết vừa được tạo</returns>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ForumPostResponse>> Create([FromBody] ForumPostCreateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { Message = "Request payload is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { Message = "Content is required." });
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { Message = "You must be logged in to create a post." });
            }

            try
            {
                var response = await _service.CreateAsync(request, currentUserId.Value);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}