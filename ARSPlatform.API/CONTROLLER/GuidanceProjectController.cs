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
    public class GuidanceProjectController : ControllerBase
    {
        private readonly IGuidanceProjectService _service;

        public GuidanceProjectController(IGuidanceProjectService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách dự án hướng dẫn nghiên cứu (có thể lọc theo researchGroupId)
        /// </summary>
        /// <param name="researchGroupId">ID nhóm nghiên cứu (tùy chọn)</param>
        /// <returns>Danh sách dự án hướng dẫn</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GuidanceProjectResponse>>> GetAll([FromQuery] int? researchGroupId = null)
        {
            var items = await _service.GetAllAsync(researchGroupId);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="researchGroupId">ID nhóm nghiên cứu (tùy chọn)</param>
        /// <returns>Danh sách dự án hướng dẫn có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<GuidanceProjectResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? researchGroupId = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, researchGroupId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới dự án hướng dẫn nghiên cứu
        /// </summary>
        /// <param name="request">Thông tin dự án hướng dẫn</param>
        /// <returns>Dự án vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<GuidanceProjectResponse>> Create([FromBody] GuidanceProjectCreateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var response = await _service.CreateAsync(request, currentUserId);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết dự án hướng dẫn theo ID
        /// </summary>
        /// <param name="id">ID dự án</param>
        /// <returns>Chi tiết dự án hướng dẫn</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<GuidanceProjectResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Guidance project not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin dự án hướng dẫn
        /// </summary>
        /// <param name="id">ID dự án cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Dự án sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<GuidanceProjectResponse>> Update(int id, [FromBody] GuidanceProjectUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Guidance project not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một dự án hướng dẫn
        /// </summary>
        /// <param name="id">ID dự án cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Guidance project not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
