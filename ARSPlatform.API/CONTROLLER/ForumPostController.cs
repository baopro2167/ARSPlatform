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

        /// <summary>
        /// Lấy danh sách tất cả các bài viết trên diễn đàn (có hỗ trợ tìm kiếm, lọc theo chủ đề và sắp xếp)
        /// </summary>
        /// <param name="category">Chuyên mục / Lĩnh vực lọc</param>
        /// <param name="sort">Kiểu sắp xếp: popular, newest</param>
        /// <param name="search">Từ khóa tìm kiếm trong tiêu đề hoặc nội dung</param>
        /// <returns>Danh sách bài viết diễn đàn</returns>
        [HttpGet]
        [Authorize(Policy = "ForumRead")]
        public async Task<ActionResult<IEnumerable<ForumPostResponse>>> GetAll(
            [FromQuery] string? category,
            [FromQuery] string? sort,
            [FromQuery] string? search)
        {
            var items = await _service.GetAllAsync(category, sort, search);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="category">Chuyên mục / Lĩnh vực lọc</param>
        /// <param name="sort">Kiểu sắp xếp: popular, newest</param>
        /// <param name="search">Từ khóa tìm kiếm trong tiêu đề hoặc nội dung</param>
        /// <returns>Danh sách bài viết diễn đàn có phân trang</returns>
        [HttpGet("paged")]
        [Authorize(Policy = "ForumRead")]
        public async Task<ActionResult<PagedResult<ForumPostResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] string? category = null,
            [FromQuery] string? sort = null,
            [FromQuery] string? search = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, category, sort, search);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một bài viết trên diễn đàn theo ID
        /// </summary>
        /// <param name="id">ID bài viết</param>
        /// <returns>Thông tin chi tiết bài viết và danh sách bình luận</returns>
        [HttpGet("{id:int}")]
        [Authorize(Policy = "ForumRead")]
        public async Task<ActionResult<ForumPostResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Forum post not found." });
            return Ok(item);
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
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { Message = "Content is required." });
            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var response = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
    }
}