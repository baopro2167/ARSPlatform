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
    public class UserRoleController : ControllerBase
    {
        private readonly IUserRoleService _service;

        public UserRoleController(IUserRoleService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách phân quyền người dùng (User - Role)
        /// </summary>
        /// <returns>Danh sách phân quyền</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserRoleResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách phân quyền người dùng có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<UserRoleResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Gán vai trò mới cho người dùng (Chỉ dành cho Admin)
        /// </summary>
        /// <param name="request">Thông tin gán vai trò</param>
        /// <returns>Bản ghi phân quyền vừa tạo</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserRoleResponse>> Create([FromBody] UserRoleCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết phân quyền theo ID
        /// </summary>
        /// <param name="id">ID bản ghi phân quyền</param>
        /// <returns>Chi tiết phân quyền</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserRoleResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "User role not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật phân quyền người dùng (Chỉ dành cho Admin)
        /// </summary>
        /// <param name="id">ID bản ghi phân quyền</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Bản ghi sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserRoleResponse>> Update(int id, [FromBody] UserRoleUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "User role not found." });
            return Ok(response);
        }

        /// <summary>
        /// Thu hồi / xóa phân quyền của người dùng (Chỉ dành cho Admin)
        /// </summary>
        /// <param name="id">ID bản ghi phân quyền</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "User role not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
