using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Authorize]
    public class ResearchGroupJoinRequestController : ControllerBase
    {
        private readonly IResearchGroupJoinRequestService _service;

        public ResearchGroupJoinRequestController(IResearchGroupJoinRequestService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        /// <summary>
        /// Học viên gửi yêu cầu xin gia nhập nhóm nghiên cứu
        /// </summary>
        /// <param name="groupId">ID nhóm nghiên cứu</param>
        /// <param name="request">Thông tin yêu cầu (ghi chú tùy chọn)</param>
        /// <returns>Thông tin yêu cầu gia nhập vừa tạo</returns>
        [HttpPost("api/ResearchGroup/{groupId:int}/join-requests")]
        public async Task<ActionResult<ResearchGroupJoinRequestResponse>> CreateJoinRequest(
            int groupId,
            [FromBody] ResearchGroupJoinRequestCreateRequest? request = null)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var response = await _service.CreateJoinRequestAsync(groupId, currentUserId.Value, request?.Note);
                return StatusCode(201, response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Conflict: already member, duplicate request, capacity reached, inactive
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Giảng viên xem danh sách yêu cầu gia nhập của nhóm nghiên cứu
        /// </summary>
        /// <param name="groupId">ID nhóm nghiên cứu</param>
        /// <param name="status">Trạng thái lọc (mặc định: PENDING)</param>
        /// <returns>Danh sách yêu cầu gia nhập kèm Profile ứng viên</returns>
        [HttpGet("api/lecturer/research-groups/{groupId:int}/join-requests")]
        public async Task<ActionResult<IEnumerable<ResearchGroupJoinRequestResponse>>> GetJoinRequests(
            int groupId,
            [FromQuery] string? status = "PENDING")
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var items = await _service.GetJoinRequestsForLecturerAsync(groupId, currentUserId.Value, status);
                return Ok(items);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Giảng viên xem chi tiết một yêu cầu gia nhập nhóm nghiên cứu
        /// </summary>
        /// <param name="groupId">ID nhóm nghiên cứu</param>
        /// <param name="joinRequestId">ID yêu cầu gia nhập</param>
        /// <returns>Chi tiết yêu cầu gia nhập</returns>
        [HttpGet("api/lecturer/research-groups/{groupId:int}/join-requests/{joinRequestId:int}")]
        public async Task<ActionResult<ResearchGroupJoinRequestResponse>> GetJoinRequestById(int groupId, int joinRequestId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var item = await _service.GetJoinRequestByIdAsync(groupId, joinRequestId, currentUserId.Value);
                if (item == null) return NotFound(new { Message = "Join request not found." });
                return Ok(item);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Giảng viên chấp thuận yêu cầu gia nhập nhóm nghiên cứu
        /// </summary>
        /// <param name="groupId">ID nhóm nghiên cứu</param>
        /// <param name="joinRequestId">ID yêu cầu gia nhập</param>
        /// <returns>Thông tin yêu cầu sau khi chấp thuận</returns>
        [HttpPost("api/lecturer/research-groups/{groupId:int}/join-requests/{joinRequestId:int}/accept")]
        public async Task<ActionResult<ResearchGroupJoinRequestResponse>> AcceptJoinRequest(int groupId, int joinRequestId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var response = await _service.AcceptJoinRequestAsync(groupId, joinRequestId, currentUserId.Value);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Giảng viên từ chối yêu cầu gia nhập nhóm nghiên cứu
        /// </summary>
        /// <param name="groupId">ID nhóm nghiên cứu</param>
        /// <param name="joinRequestId">ID yêu cầu gia nhập</param>
        /// <param name="request">Lý do từ chối (tùy chọn)</param>
        /// <returns>Thông tin yêu cầu sau khi từ chối</returns>
        [HttpPost("api/lecturer/research-groups/{groupId:int}/join-requests/{joinRequestId:int}/reject")]
        public async Task<ActionResult<ResearchGroupJoinRequestResponse>> RejectJoinRequest(
            int groupId,
            int joinRequestId,
            [FromBody] RejectJoinRequestRequest? request = null)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var response = await _service.RejectJoinRequestAsync(groupId, joinRequestId, request?.RejectionNote, currentUserId.Value);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
