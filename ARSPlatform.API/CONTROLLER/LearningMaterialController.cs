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
    public class LearningMaterialController : ControllerBase
    {
        private readonly ILearningMaterialService _service;

        public LearningMaterialController(ILearningMaterialService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ tài liệu học tập
        /// </summary>
        /// <returns>Danh sách tài liệu</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LearningMaterialResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Tạo mới tài liệu học tập
        /// </summary>
        /// <param name="request">Thông tin tài liệu</param>
        /// <returns>Tài liệu vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<LearningMaterialResponse>> Create([FromBody] LearningMaterialCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết tài liệu học tập theo ID
        /// </summary>
        /// <param name="id">ID tài liệu</param>
        /// <returns>Chi tiết tài liệu</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<LearningMaterialResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Learning material not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin tài liệu học tập
        /// </summary>
        /// <param name="id">ID tài liệu</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Tài liệu sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<LearningMaterialResponse>> Update(int id, [FromBody] LearningMaterialUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Learning material not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một tài liệu học tập
        /// </summary>
        /// <param name="id">ID tài liệu</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Learning material not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
