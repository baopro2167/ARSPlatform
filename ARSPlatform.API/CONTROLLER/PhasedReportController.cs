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
    public class PhasedReportController : ControllerBase
    {
        private readonly IPhasedReportService _service;

        public PhasedReportController(IPhasedReportService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách báo cáo tiến độ theo giai đoạn
        /// </summary>
        /// <returns>Danh sách báo cáo tiến độ</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhasedReportResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Tạo mới báo cáo tiến độ theo giai đoạn
        /// </summary>
        /// <param name="request">Thông tin báo cáo tiến độ</param>
        /// <returns>Báo cáo vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<PhasedReportResponse>> Create([FromBody] PhasedReportCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết báo cáo tiến độ theo ID
        /// </summary>
        /// <param name="id">ID báo cáo</param>
        /// <returns>Chi tiết báo cáo tiến độ</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PhasedReportResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Phased report not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin báo cáo tiến độ
        /// </summary>
        /// <param name="id">ID báo cáo cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Báo cáo sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<PhasedReportResponse>> Update(int id, [FromBody] PhasedReportUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Phased report not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một báo cáo tiến độ
        /// </summary>
        /// <param name="id">ID báo cáo cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Phased report not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
