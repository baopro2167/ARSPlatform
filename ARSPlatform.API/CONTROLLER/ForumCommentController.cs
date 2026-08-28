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
    public class ForumCommentController : ControllerBase
    {
        private readonly IForumCommentService _service;

        public ForumCommentController(IForumCommentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ bình luận trên diễn đàn
        /// </summary>
        /// <param name="postId">Lọc theo ID bài viết (tùy chọn)</param>
        /// <returns>Danh sách bình luận</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ForumCommentResponse>>> GetAll([FromQuery] int? postId = null)
        {
            var items = await _service.GetAllAsync(postId);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="postId">Lọc theo ID bài viết (tùy chọn)</param>
        /// <returns>Danh sách bình luận có phân trang</returns>
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ForumCommentResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? postId = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, postId);
            return Ok(result);
        }

        /// <summary>
        /// Thêm bình luận mới vào bài viết diễn đàn
        /// </summary>
        /// <param name="request">Thông tin bình luận</param>
        /// <returns>Bình luận vừa tạo</returns>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ForumCommentResponse>> Create([FromBody] ForumCommentCreateRequest request)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var response = await _service.CreateAsync(request, userId);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết bình luận theo ID
        /// </summary>
        /// <param name="id">ID bình luận</param>
        /// <returns>Thông tin bình luận</returns>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ForumCommentResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Comment not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật nội dung bình luận
        /// </summary>
        /// <param name="id">ID bình luận cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Bình luận sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<ForumCommentResponse>> Update(int id, [FromBody] ForumCommentUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Comment not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một bình luận
        /// </summary>
        /// <param name="id">ID bình luận cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Comment not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}