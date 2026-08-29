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
    [Authorize(Roles = "Admin")]
    public class AnnualFeeController : ControllerBase
    {
        private readonly IAnnualFeeService _service;

        public AnnualFeeController(IAnnualFeeService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách phí thường niên / gói dịch vụ (Admin only)
        /// </summary>
        /// <param name="isActive">Lọc theo trạng thái kích hoạt (tùy chọn)</param>
        /// <param name="targetRole">Lọc theo vai trò (tùy chọn)</param>
        /// <param name="billingCycle">Lọc theo chu kỳ thanh toán (tùy chọn)</param>
        /// <returns>Danh sách phí thường niên</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AnnualFeeResponse>>> GetAll(
            [FromQuery] bool? isActive = null,
            [FromQuery] string? targetRole = null,
            [FromQuery] string? billingCycle = null)
        {
            var items = await _service.GetAllAsync(isActive, targetRole, billingCycle);
            return Ok(items);
        }

        /// <summary>
        /// Lấy chi tiết phí thường niên theo ID (Admin only)
        /// </summary>
        /// <param name="id">ID phí thường niên</param>
        /// <returns>Chi tiết phí thường niên</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AnnualFeeResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { Message = $"Annual fee with ID {id} not found." });
            }
            return Ok(item);
        }

        /// <summary>
        /// Tạo mới phí thường niên (Admin only)
        /// </summary>
        /// <param name="request">Thông tin phí thường niên</param>
        /// <returns>Phí thường niên vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<AnnualFeeResponse>> Create([FromBody] AnnualFeeCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var created = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật phí thường niên (Admin only)
        /// </summary>
        /// <param name="id">ID phí thường niên</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Phí thường niên sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<AnnualFeeResponse>> Update(int id, [FromBody] AnnualFeeUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updated = await _service.UpdateAsync(id, request);
                if (updated == null)
                {
                    return NotFound(new { Message = $"Annual fee with ID {id} not found." });
                }
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Bật / Tắt trạng thái kích hoạt của phí thường niên (Admin only)
        /// </summary>
        /// <param name="id">ID phí thường niên</param>
        /// <returns>Phí thường niên sau khi toggle</returns>
        [HttpPatch("{id:int}/toggle")]
        public async Task<ActionResult<AnnualFeeResponse>> ToggleActive(int id)
        {
            try
            {
                var updated = await _service.ToggleActiveAsync(id);
                if (updated == null)
                {
                    return NotFound(new { Message = $"Annual fee with ID {id} not found." });
                }
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa hoặc vô hiệu hóa phí thường niên (Admin only)
        /// </summary>
        /// <param name="id">ID phí thường niên</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new { Message = $"Annual fee with ID {id} not found." });
            }
            return Ok(new { Message = "Annual fee deleted or deactivated successfully." });
        }
    }
}
