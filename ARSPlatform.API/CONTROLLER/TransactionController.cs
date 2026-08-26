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
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _service;

        public TransactionController(ITransactionService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách giao dịch
        /// </summary>
        /// <returns>Danh sách giao dịch</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Tạo mới một giao dịch
        /// </summary>
        /// <param name="request">Thông tin giao dịch</param>
        /// <returns>Giao dịch vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<TransactionResponse>> Create([FromBody] TransactionCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết giao dịch theo ID
        /// </summary>
        /// <param name="id">ID giao dịch</param>
        /// <returns>Chi tiết giao dịch</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TransactionResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Transaction not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin giao dịch
        /// </summary>
        /// <param name="id">ID giao dịch cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Giao dịch sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<TransactionResponse>> Update(int id, [FromBody] TransactionUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Transaction not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một giao dịch
        /// </summary>
        /// <param name="id">ID giao dịch cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Transaction not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
