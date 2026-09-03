using System.Collections.Generic;
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
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách yêu cầu phản biện có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ReviewRequestResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Tự động tìm và phân công phản biện viên phù hợp cho bài báo theo SubFieldId và các ràng buộc tải công việc
        /// </summary>
        /// <param name="request">Thông tin PaperId và số lượng Reviewer cần thiết (ví dụ: 1, 3, 5)</param>
        /// <returns>Danh sách phản biện viên được phân công kèm trạng thái PENDING và thông báo gửi</returns>
        [HttpPost("auto-assign")]
        public async Task<ActionResult<AutoAssignReviewersResponse>> AutoAssign([FromBody] AutoAssignReviewersRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _service.AutoAssignReviewersAsync(request);
                return Ok(response);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (System.Collections.Generic.KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gán thủ công danh sách phản biện viên (ví dụ: 3 Reviewer) cho một bài báo nghiên cứu khoa học
        /// </summary>
        /// <param name="request">Thông tin PaperId và danh sách ReviewerIds (hoặc reviewerId1, reviewerId2, reviewerId3)</param>
        /// <returns>Danh sách các Reviewer được gán thủ công và trạng thái tạo trong ReviewRequest</returns>
        [HttpPost("manual-assign")]
        public async Task<ActionResult<ManualAssignReviewersResponse>> ManualAssign([FromBody] ManualAssignReviewersRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _service.ManualAssignReviewersAsync(request);
                return Ok(response);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (System.Collections.Generic.KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
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