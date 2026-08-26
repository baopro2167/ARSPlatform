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
    public class GroupMemberController : ControllerBase
    {
        private readonly IGroupMemberService _service;

        public GroupMemberController(IGroupMemberService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ thành viên thuộc các nhóm nghiên cứu
        /// </summary>
        /// <param name="groupId">Lọc theo ID nhóm (tùy chọn)</param>
        /// <returns>Danh sách thành viên nhóm</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupMemberResponse>>> GetAll([FromQuery] int? groupId = null)
        {
            var items = await _service.GetAllAsync(groupId);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="groupId">Lọc theo ID nhóm (tùy chọn)</param>
        /// <returns>Danh sách thành viên nhóm có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<GroupMemberResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? groupId = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, groupId);
            return Ok(result);
        }

        /// <summary>
        /// Thêm thành viên vào nhóm nghiên cứu
        /// </summary>
        /// <param name="request">Thông tin thành viên</param>
        /// <returns>Bản ghi thành viên vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<GroupMemberResponse>> Create([FromBody] GroupMemberCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết thành viên nhóm theo ID
        /// </summary>
        /// <param name="id">ID thành viên nhóm</param>
        /// <returns>Chi tiết thành viên nhóm</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<GroupMemberResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Group member not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật vai trò / thông tin thành viên trong nhóm
        /// </summary>
        /// <param name="id">ID bản ghi thành viên</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thành viên sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<GroupMemberResponse>> Update(int id, [FromBody] GroupMemberUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Group member not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa thành viên khỏi nhóm nghiên cứu
        /// </summary>
        /// <param name="id">ID bản ghi thành viên</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Group member not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
