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
    [Authorize]
    public class DetailedEvaluationController : ControllerBase
    {
        private readonly IDetailedEvaluationService _service;

        public DetailedEvaluationController(IDetailedEvaluationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách phiếu đánh giá chi tiết bài báo
        /// </summary>
        /// <returns>Danh sách phiếu đánh giá</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DetailedEvaluationResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách phiếu đánh giá chi tiết bài báo có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<DetailedEvaluationResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới phiếu đánh giá chi tiết bài báo (theo khung Rubric chuyên ngành)
        /// </summary>
        /// <param name="request">Thông tin đánh giá chi tiết</param>
        /// <returns>Phiếu đánh giá vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<DetailedEvaluationResponse>> Create([FromBody] DetailedEvaluationCreateRequest request)
        {
            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdValue, out var currentUserId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _service.CreateAsync(request, currentUserId);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết phiếu đánh giá theo ID
        /// </summary>
        /// <param name="id">ID phiếu đánh giá</param>
        /// <returns>Chi tiết phiếu đánh giá</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DetailedEvaluationResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Detailed evaluation not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật phiếu đánh giá chi tiết
        /// </summary>
        /// <param name="id">ID phiếu đánh giá cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Phiếu đánh giá sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<DetailedEvaluationResponse>> Update(
            int id,
            [FromBody] DetailedEvaluationUpdateRequest request)
        {
            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdValue, out var currentUserId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _service.UpdateAsync(id, request, currentUserId);
                if (response == null) return NotFound(new { Message = "Detailed evaluation not found." });
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa một phiếu đánh giá chi tiết
        /// </summary>
        /// <param name="id">ID phiếu đánh giá</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Detailed evaluation not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
