using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewerController : ControllerBase
    {
        private readonly IReviewWorkflowService _workflowService;

        public ReviewerController(IReviewWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        private int GetCurrentUserId()
        {
            var userIdVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdVal, out var id)) return id;
            throw new UnauthorizedAccessException("Không thể xác thực thông tin người dùng từ token.");
        }

        /// <summary>
        /// Lấy danh sách các bài báo được phân công cho Reviewer hiện tại
        /// </summary>
        [HttpGet("assignments")]
        public async Task<ActionResult<IEnumerable<ReviewerAssignmentResponse>>> GetMyAssignments()
        {
            var reviewerId = GetCurrentUserId();
            var assignments = await _workflowService.GetAssignmentsForReviewerAsync(reviewerId);
            return Ok(assignments);
        }

        /// <summary>
        /// Lấy chi tiết bài báo và trạng thái phản biện của một phân công
        /// </summary>
        [HttpGet("assignments/{id:int}")]
        public async Task<ActionResult<ReviewerAssignmentResponse>> GetAssignmentById(int id)
        {
            var reviewerId = GetCurrentUserId();
            var assignment = await _workflowService.GetAssignmentByIdAsync(id, reviewerId);
            if (assignment == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu phân công phản biện này hoặc bạn không có quyền truy cập." });

            return Ok(assignment);
        }

        /// <summary>
        /// Reviewer chấp nhận yêu cầu phản biện (chuyển sang trạng thái UNDER_REVIEW)
        /// </summary>
        [HttpPost("assignments/{id:int}/accept")]
        public async Task<ActionResult<ReviewerAssignmentResponse>> AcceptAssignment(int id)
        {
            try
            {
                var reviewerId = GetCurrentUserId();
                var result = await _workflowService.AcceptAssignmentAsync(id, reviewerId);
                return Ok(new { message = "Đã chấp nhận lời mời phản biện bài báo thành công.", data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Reviewer từ chối yêu cầu phản biện (kèm lý do)
        /// </summary>
        [HttpPost("assignments/{id:int}/decline")]
        public async Task<ActionResult<ReviewerAssignmentResponse>> DeclineAssignment(int id, [FromBody] ReviewerDeclineRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var reviewerId = GetCurrentUserId();
                var result = await _workflowService.DeclineAssignmentAsync(id, reviewerId, request);
                return Ok(new { message = "Đã từ chối phản biện bài báo thành công.", data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Reviewer khai báo xung đột lợi ích (Conflict of Interest - COI)
        /// </summary>
        [HttpPost("assignments/{id:int}/conflict-of-interest")]
        public async Task<ActionResult<ReviewerAssignmentResponse>> DeclareConflictOfInterest(int id, [FromBody] ReviewerConflictOfInterestRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var reviewerId = GetCurrentUserId();
                var result = await _workflowService.DeclareConflictOfInterestAsync(id, reviewerId, request);
                return Ok(new { message = "Đã ghi nhận khai báo xung đột lợi ích và hủy phân công thành công.", data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Reviewer nộp phiếu đánh giá chi tiết (11 tiêu chí học thuật, nhận xét và đề xuất kết quả)
        /// </summary>
        [HttpPost("assignments/{id:int}/submit-review")]
        public async Task<ActionResult<PaperReviewResponse>> SubmitReview(int id, [FromBody] PaperReviewSubmitRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var reviewerId = GetCurrentUserId();
                var result = await _workflowService.SubmitReviewAsync(id, reviewerId, request);
                return Ok(new { message = "Đã nộp kết quả đánh giá phản biện bài báo thành công.", data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
