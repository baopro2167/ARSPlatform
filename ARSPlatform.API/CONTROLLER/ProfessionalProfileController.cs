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
    public class ProfessionalProfileController : ControllerBase
    {
        private readonly IProfessionalProfileService _service;

        public ProfessionalProfileController(IProfessionalProfileService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ hồ sơ chuyên môn (kèm thông tin chuyên ngành và tài khoản)
        /// </summary>
        /// <returns>Danh sách hồ sơ chuyên môn</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProfessionalProfileResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách hồ sơ chuyên môn có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ProfessionalProfileResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Tạo hồ sơ chuyên môn cho người dùng
        /// </summary>
        /// <param name="request">Thông tin hồ sơ chuyên môn</param>
        /// <returns>Hồ sơ chuyên môn vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<ProfessionalProfileResponse>> Create([FromBody] ProfessionalProfileCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.UserId }, response);
        }

        /// <summary>
        /// Lấy chi tiết hồ sơ chuyên môn theo ID người dùng
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Chi tiết hồ sơ chuyên môn</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProfessionalProfileResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Professional profile not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ chuyên môn
        /// </summary>
        /// <param name="id">User ID cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Hồ sơ chuyên môn sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProfessionalProfileResponse>> Update(int id, [FromBody] ProfessionalProfileUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Professional profile not found." });
            return Ok(response);
        }

        /// <summary>
        /// Cập nhật trạng thái sẵn sàng phản biện của Reviewer
        /// </summary>
        /// <param name="id">User ID của Reviewer</param>
        /// <param name="request">Trạng thái sẵn sàng</param>
        /// <returns>Hồ sơ chuyên môn cập nhật</returns>
        [HttpPatch("{id:int}/availability")]
        [Authorize(Roles = "Reviewer")]
        public async Task<ActionResult<ProfessionalProfileResponse>> UpdateAvailability(
            int id,
            [FromBody] ProfessionalProfileAvailabilityUpdateRequest request)
        {
            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdValue, out var currentUserId))
            {
                return Unauthorized();
            }

            if (currentUserId != id)
            {
                return Forbid();
            }

            var response = await _service.UpdateAvailabilityAsync(id, request.IsAvailable);
            if (response == null) return NotFound(new { Message = "Professional profile not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa hồ sơ chuyên môn của người dùng
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Không có nội dung</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Professional profile not found." });
            return NoContent();
        }
    }
}