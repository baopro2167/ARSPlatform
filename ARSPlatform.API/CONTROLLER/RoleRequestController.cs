using System.Collections.Generic;
using System.Security.Claims;
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
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RoleRequestController : ControllerBase
    {
        private readonly IRoleRequestService _roleRequestService;

        public RoleRequestController(
            IRoleRequestService roleRequestService)
        {
            _roleRequestService = roleRequestService;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ yêu cầu cấp vai trò (Role Requests) đang chờ Admin xử lý
        /// </summary>
        /// <returns>Danh sách yêu cầu cấp quyền</returns>
        [HttpGet]
        [ProducesResponseType(
            typeof(IEnumerable<RoleRequestResponse>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var items = await _roleRequestService.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách yêu cầu cấp quyền có phân trang</returns>
        [HttpGet("paged")]
        [ProducesResponseType(
            typeof(PagedResult<RoleRequestResponse>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<RoleRequestResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _roleRequestService.GetPagedAsync(paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết yêu cầu cấp vai trò theo ID
        /// </summary>
        /// <param name="id">ID yêu cầu cấp vai trò</param>
        /// <returns>Chi tiết yêu cầu</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _roleRequestService.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound(new
                {
                    Message = $"Role request {id} was not found."
                });
            }

            return Ok(item);
        }

        /// <summary>
        /// Phê duyệt yêu cầu cấp vai trò người dùng (Chuyển trạng thái Approved và gán Role)
        /// </summary>
        /// <param name="id">ID yêu cầu cấp vai trò</param>
        /// <param name="request">Ghi chú quyết định</param>
        /// <returns>Kết quả phê duyệt</returns>
        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(
            int id,
            [FromBody] RoleRequestDecisionRequest? request)
        {
            var adminId = GetCurrentAdminId();

            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            request ??= new RoleRequestDecisionRequest();

            try
            {
                var result = await _roleRequestService.ApproveAsync(
                    id,
                    adminId.Value,
                    request);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
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
        /// Từ chối yêu cầu cấp vai trò người dùng
        /// </summary>
        /// <param name="id">ID yêu cầu cấp vai trò</param>
        /// <param name="request">Lý do từ chối</param>
        /// <returns>Kết quả từ chối</returns>
        [HttpPost("{id:int}/deny")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Deny(
            int id,
            [FromBody] RoleRequestDecisionRequest? request)
        {
            var adminId = GetCurrentAdminId();

            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    Message = "Request body is required."
                });
            }

            try
            {
                var result = await _roleRequestService.DenyAsync(
                    id,
                    adminId.Value,
                    request);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
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

        private int? GetCurrentAdminId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(value, out var adminId)
                ? adminId
                : null;
        }
    }
}
