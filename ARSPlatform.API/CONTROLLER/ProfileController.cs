using System;
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
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _service;

        public ProfileController(IProfileService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ hồ sơ người dùng kèm thông tin tài khoản
        /// </summary>
        /// <returns>Danh sách hồ sơ</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProfileResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách hồ sơ người dùng có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ProfileResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Tạo hồ sơ cá nhân cho người dùng
        /// </summary>
        /// <param name="request">Thông tin hồ sơ cần tạo</param>
        /// <returns>Hồ sơ cá nhân vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<ProfileResponse>> Create([FromBody] ProfileCreateRequest request)
        {
            try
            {
                var response = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = request.UserId }, response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin hồ sơ theo ID người dùng
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Chi tiết hồ sơ</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProfileResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Profile not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ cá nhân
        /// </summary>
        /// <param name="id">User ID cần cập nhật</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Hồ sơ sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        [HttpPatch("{id:int}")]
        public async Task<ActionResult<ProfileResponse>> Update(int id, [FromBody] ProfileUpdateRequest request)
        {
            try
            {
                var response = await _service.UpdateAsync(id, request);
                if (response == null) return NotFound(new { Message = "Profile not found." });
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa hồ sơ cá nhân của người dùng
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Không có nội dung</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Profile not found." });
            return NoContent();
        }
    }
}