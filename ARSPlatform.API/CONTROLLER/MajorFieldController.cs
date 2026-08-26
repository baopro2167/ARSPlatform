using System;
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
    public class MajorFieldController : ControllerBase
    {
        private readonly IMajorFieldService _service;

        public MajorFieldController(IMajorFieldService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ các lĩnh vực nghiên cứu lớn (kèm các chuyên ngành hẹp)
        /// </summary>
        /// <returns>Danh sách lĩnh vực nghiên cứu</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MajorFieldResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Tạo mới một lĩnh vực nghiên cứu lớn
        /// </summary>
        /// <param name="request">Thông tin lĩnh vực nghiên cứu</param>
        /// <returns>Lĩnh vực nghiên cứu vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<MajorFieldResponse>> Create([FromBody] MajorFieldCreateRequest request)
        {
            try
            {
                var response = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = response.MajorFieldId }, response);
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
        /// Lấy chi tiết lĩnh vực nghiên cứu theo ID
        /// </summary>
        /// <param name="id">ID lĩnh vực</param>
        /// <returns>Chi tiết lĩnh vực</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MajorFieldResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Major field not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật tên lĩnh vực nghiên cứu
        /// </summary>
        /// <param name="id">ID lĩnh vực cần cập nhật</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Lĩnh vực sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<MajorFieldResponse>> Update(int id, [FromBody] MajorFieldUpdateRequest request)
        {
            try
            {
                var response = await _service.UpdateAsync(id, request);
                if (response == null) return NotFound(new { Message = "Major field not found." });
                return Ok(response);
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
        /// Xóa một lĩnh vực nghiên cứu lớn
        /// </summary>
        /// <param name="id">ID lĩnh vực</param>
        /// <returns>Không có nội dung</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.DeleteAsync(id);
                if (!success) return NotFound(new { Message = "Major field not found." });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }
    }
}