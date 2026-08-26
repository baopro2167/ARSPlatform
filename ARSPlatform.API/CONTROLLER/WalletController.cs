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
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _service;

        public WalletController(IWalletService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy thông tin ví của người dùng (hoặc danh sách ví)
        /// </summary>
        /// <param name="userId">ID người dùng cần xem ví</param>
        /// <returns>Danh sách thông tin ví</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WalletResponse>>> GetAll([FromQuery] int? userId)
        {
            var items = await _service.GetAllAsync(userId);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="userId">ID người dùng cần lọc ví (tùy chọn)</param>
        /// <returns>Danh sách thông tin ví có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<WalletResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? userId = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, userId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới ví cho người dùng
        /// </summary>
        /// <param name="request">Thông tin ví</param>
        /// <returns>Ví vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<WalletResponse>> Create([FromBody] WalletCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết ví theo WalletId
        /// </summary>
        /// <param name="id">ID ví</param>
        /// <returns>Chi tiết ví</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<WalletResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Wallet not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin ví
        /// </summary>
        /// <param name="id">ID ví cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Ví sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<WalletResponse>> Update(int id, [FromBody] WalletUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Wallet not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một ví
        /// </summary>
        /// <param name="id">ID ví cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Wallet not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}