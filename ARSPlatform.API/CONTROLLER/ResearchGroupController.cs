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
    public class ResearchGroupController : ControllerBase
    {
        private readonly IResearchGroupService _service;

        public ResearchGroupController(IResearchGroupService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách nhóm nghiên cứu
        /// </summary>
        /// <returns>Danh sách nhóm nghiên cứu</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResearchGroupResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách nhóm nghiên cứu có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ResearchGroupResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Tạo nhóm nghiên cứu mới
        /// </summary>
        /// <param name="request">Thông tin nhóm nghiên cứu</param>
        /// <returns>Nhóm nghiên cứu vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<ResearchGroupResponse>> Create([FromBody] ResearchGroupCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết nhóm nghiên cứu theo ID
        /// </summary>
        /// <param name="id">ID nhóm nghiên cứu</param>
        /// <returns>Chi tiết nhóm nghiên cứu</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResearchGroupResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Research group not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin nhóm nghiên cứu
        /// </summary>
        /// <param name="id">ID nhóm nghiên cứu</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Nhóm nghiên cứu sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ResearchGroupResponse>> Update(int id, [FromBody] ResearchGroupUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Research group not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một nhóm nghiên cứu
        /// </summary>
        /// <param name="id">ID nhóm nghiên cứu</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Research group not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
