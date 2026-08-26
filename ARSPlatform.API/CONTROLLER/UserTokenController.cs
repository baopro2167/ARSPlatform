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
    public class UserTokenController : ControllerBase
    {
        private readonly IUserTokenService _service;

        public UserTokenController(IUserTokenService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ mã Token của người dùng
        /// </summary>
        /// <returns>Danh sách User Token</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserTokenResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Tạo mới một User Token
        /// </summary>
        /// <param name="request">Thông tin User Token</param>
        /// <returns>User Token vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<UserTokenResponse>> Create([FromBody] UserTokenCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết User Token theo ID
        /// </summary>
        /// <param name="id">ID của User Token</param>
        /// <returns>Chi tiết User Token</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserTokenResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "User token not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin User Token
        /// </summary>
        /// <param name="id">ID User Token</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>User Token sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserTokenResponse>> Update(int id, [FromBody] UserTokenUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "User token not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một User Token
        /// </summary>
        /// <param name="id">ID User Token cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "User token not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
