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
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ thông báo hệ thống
        /// </summary>
        /// <param name="userId">Lọc theo ID người dùng (tùy chọn)</param>
        /// <returns>Danh sách thông báo</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetAll([FromQuery] int? userId = null)
        {
            var items = await _service.GetAllAsync(userId);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="userId">Lọc theo ID người dùng (tùy chọn)</param>
        /// <returns>Danh sách thông báo có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<NotificationResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? userId = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, userId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới một thông báo
        /// </summary>
        /// <param name="request">Dữ liệu thông báo</param>
        /// <returns>Thông báo vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<NotificationResponse>> Create([FromBody] NotificationCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết thông báo theo ID
        /// </summary>
        /// <param name="id">ID thông báo</param>
        /// <returns>Chi tiết thông báo</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<NotificationResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Notification not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông báo (đánh dấu đã đọc...)
        /// </summary>
        /// <param name="id">ID thông báo</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Thông báo sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<NotificationResponse>> Update(int id, [FromBody] NotificationUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Notification not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một thông báo
        /// </summary>
        /// <param name="id">ID thông báo cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Notification not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
