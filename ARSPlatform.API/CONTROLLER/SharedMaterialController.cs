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
    public class SharedMaterialController : ControllerBase
    {
        private readonly ISharedMaterialService _service;

        public SharedMaterialController(ISharedMaterialService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách tài liệu chia sẻ
        /// </summary>
        /// <returns>Danh sách tài liệu chia sẻ</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SharedMaterialResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Chia sẻ tài liệu mới
        /// </summary>
        /// <param name="request">Thông tin tài liệu chia sẻ</param>
        /// <returns>Bản ghi chia sẻ vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<SharedMaterialResponse>> Create([FromBody] SharedMaterialCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết tài liệu chia sẻ theo ID
        /// </summary>
        /// <param name="id">ID bản ghi chia sẻ</param>
        /// <returns>Chi tiết tài liệu chia sẻ</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SharedMaterialResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Shared material not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin chia sẻ tài liệu
        /// </summary>
        /// <param name="id">ID bản ghi chia sẻ</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Bản ghi chia sẻ sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<SharedMaterialResponse>> Update(int id, [FromBody] SharedMaterialUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Shared material not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một tài liệu chia sẻ
        /// </summary>
        /// <param name="id">ID bản ghi chia sẻ</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Shared material not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
