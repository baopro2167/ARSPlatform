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
    public class SubFieldController : ControllerBase
    {
        private readonly ISubFieldService _service;

        public SubFieldController(ISubFieldService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ các chuyên ngành hẹp (có thể lọc theo lĩnh vực lớn)
        /// </summary>
        /// <param name="majorFieldId">ID lĩnh vực lớn để lọc</param>
        /// <returns>Danh sách chuyên ngành hẹp</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubFieldResponse>>> GetAll([FromQuery] int? majorFieldId = null)
        {
            try
            {
                var items = await _service.GetAllAsync(majorFieldId);
                return Ok(items);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo mới một chuyên ngành hẹp
        /// </summary>
        /// <param name="request">Thông tin chuyên ngành hẹp</param>
        /// <returns>Chuyên ngành vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<SubFieldResponse>> Create([FromBody] SubFieldCreateRequest request)
        {
            try
            {
                var response = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = response.SubFieldId }, response);
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
        /// Lấy chi tiết chuyên ngành hẹp theo ID
        /// </summary>
        /// <param name="id">ID chuyên ngành hẹp</param>
        /// <returns>Chi tiết chuyên ngành</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SubFieldResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Sub-field not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin chuyên ngành hẹp
        /// </summary>
        /// <param name="id">ID chuyên ngành hẹp cần cập nhật</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Chuyên ngành sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<SubFieldResponse>> Update(int id, [FromBody] SubFieldUpdateRequest request)
        {
            try
            {
                var response = await _service.UpdateAsync(id, request);
                if (response == null) return NotFound(new { Message = "Sub-field not found." });
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
        /// Xóa một chuyên ngành hẹp
        /// </summary>
        /// <param name="id">ID chuyên ngành</param>
        /// <returns>Không có nội dung</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.DeleteAsync(id);
                if (!success) return NotFound(new { Message = "Sub-field not found." });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }
    }
}