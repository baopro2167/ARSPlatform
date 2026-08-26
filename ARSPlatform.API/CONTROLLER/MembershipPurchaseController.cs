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
    public class MembershipPurchaseController : ControllerBase
    {
        private readonly IMembershipPurchaseService _service;

        public MembershipPurchaseController(IMembershipPurchaseService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ lịch sử mua gói thành viên
        /// </summary>
        /// <returns>Danh sách lịch sử mua gói</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MembershipPurchaseResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Tạo bản ghi mua gói thành viên mới
        /// </summary>
        /// <param name="request">Thông tin mua gói</param>
        /// <returns>Bản ghi mua gói vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<MembershipPurchaseResponse>> Create([FromBody] MembershipPurchaseCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết lịch sử mua gói theo ID
        /// </summary>
        /// <param name="id">ID bản ghi</param>
        /// <returns>Chi tiết bản ghi mua gói</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MembershipPurchaseResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Membership purchase not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin mua gói
        /// </summary>
        /// <param name="id">ID bản ghi cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Bản ghi sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<MembershipPurchaseResponse>> Update(int id, [FromBody] MembershipPurchaseUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Membership purchase not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một bản ghi mua gói thành viên
        /// </summary>
        /// <param name="id">ID bản ghi</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Membership purchase not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
