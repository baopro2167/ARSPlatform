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
    public class ReportController : ControllerBase
    {
        private readonly IReportService _service;

        public ReportController(IReportService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách báo cáo vi phạm / tố cáo nội dung
        /// </summary>
        /// <returns>Danh sách báo cáo vi phạm</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReportResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Gửi báo cáo vi phạm / tố cáo nội dung mới
        /// </summary>
        /// <param name="request">Thông tin báo cáo vi phạm</param>
        /// <returns>Báo cáo vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<ReportResponse>> Create([FromBody] ReportCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết báo cáo vi phạm theo ID
        /// </summary>
        /// <param name="id">ID báo cáo vi phạm</param>
        /// <returns>Chi tiết báo cáo</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReportResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Report not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật trạng thái / thông tin xử lý báo cáo vi phạm
        /// </summary>
        /// <param name="id">ID báo cáo cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Báo cáo sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ReportResponse>> Update(int id, [FromBody] ReportUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Report not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một báo cáo vi phạm
        /// </summary>
        /// <param name="id">ID báo cáo cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Report not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
