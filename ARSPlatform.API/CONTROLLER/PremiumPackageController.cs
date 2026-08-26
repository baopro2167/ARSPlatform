using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/PremiumPackage")]
    [Authorize(Roles = "Admin")]
    public class PremiumPackageController : ControllerBase
    {
        private readonly IPremiumPackageService _service;

        public PremiumPackageController(IPremiumPackageService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách gói Premium cho Admin quản lý (kèm số lượng người đăng ký)
        /// </summary>
        /// <returns>Danh sách gói Premium</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PremiumPackageResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách gói Premium có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<PremiumPackageResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới gói Premium
        /// </summary>
        /// <param name="request">Thông tin gói Premium</param>
        /// <returns>Gói Premium vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<PremiumPackageResponse>> Create([FromBody] PremiumPackageCreateRequest request)
        {
            try
            {
                var response = await _service.CreateAsync(request);
                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin gói Premium
        /// </summary>
        /// <param name="id">ID gói Premium</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Gói Premium sau khi cập nhật</returns>
        [HttpPatch("{id:int}")]
        public async Task<ActionResult<PremiumPackageResponse>> Update(int id, [FromBody] PremiumPackageUpdateRequest request)
        {
            try
            {
                var response = await _service.UpdateAsync(id, request);
                if (response == null) return NotFound(new { Message = "Package not found." });
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Bật/Tắt trạng thái kích hoạt của gói Premium
        /// </summary>
        /// <param name="id">ID gói Premium</param>
        /// <param name="request">Trạng thái kích hoạt</param>
        /// <returns>Gói Premium cập nhật</returns>
        [HttpPost("{id:int}/toggle")]
        public async Task<ActionResult<PremiumPackageResponse>> Toggle(int id, [FromBody] PremiumPackageToggleRequest request)
        {
            if (request == null || !request.IsActive.HasValue)
            {
                return BadRequest(new { Message = "isActive is required." });
            }

            var response = await _service.ToggleAsync(id, request.IsActive.Value);
            if (response == null) return NotFound(new { Message = "Package not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một gói Premium (chỉ xóa khi chưa có lịch sử mua)
        /// </summary>
        /// <param name="id">ID gói Premium</param>
        /// <returns>Không có nội dung</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.DeleteAsync(id);
                if (!success) return NotFound(new { Message = "Package not found." });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }
    }
}