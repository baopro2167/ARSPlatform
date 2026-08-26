using System.Collections.Generic;
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
    [Authorize]
    public class ReviewRequestController : ControllerBase
    {
        private readonly IReviewRequestService _service;

        public ReviewRequestController(IReviewRequestService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách yêu cầu phản biện bài báo khoa học (kèm thông tin Reviewer)
        /// </summary>
        /// <returns>Danh sách yêu cầu phản biện</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReviewRequestResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Tạo mới một yêu cầu phản biện bài báo
        /// </summary>
        /// <param name="request">Thông tin yêu cầu phản biện</param>
        /// <returns>Yêu cầu phản biện vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<ReviewRequestResponse>> Create([FromBody] ReviewRequestCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.ReviewRequestId }, response);
        }

        /// <summary>
        /// Lấy chi tiết yêu cầu phản biện theo ID
        /// </summary>
        /// <param name="id">ID yêu cầu phản biện</param>
        /// <returns>Chi tiết yêu cầu phản biện</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReviewRequestResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Review request not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin yêu cầu phản biện
        /// </summary>
        /// <param name="id">ID yêu cầu phản biện</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Yêu cầu phản biện sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ReviewRequestResponse>> Update(int id, [FromBody] ReviewRequestUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Review request not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một yêu cầu phản biện
        /// </summary>
        /// <param name="id">ID yêu cầu phản biện</param>
        /// <returns>Không có nội dung</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Review request not found." });
            return NoContent();
        }
    }
}