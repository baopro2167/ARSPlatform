using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
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
        /// <returns>Danh sách bình luận</returns>
        [HttpGet]
        [Authorize(Policy = "ForumRead")]
        public async Task<ActionResult<IEnumerable<ForumCommentResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
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
        [Authorize(Policy = "ForumRead")]
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