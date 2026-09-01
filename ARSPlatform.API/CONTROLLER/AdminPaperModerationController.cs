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
    [Route("api/admin/paper-submissions")]
    [Authorize(Roles = "Admin")]
    public class AdminPaperModerationController : ControllerBase
    {
        private readonly IReviewWorkflowService _workflowService;

        public AdminPaperModerationController(IReviewWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        private int GetCurrentUserId()
        {
            var userIdVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdVal, out var id)) return id;
            throw new UnauthorizedAccessException("Không thể xác thực thông tin Admin.");
        }

        /// <summary>
        /// Admin xem toàn bộ các đánh giá, tiêu chí, khuyến nghị và ghi chú mật của các Reviewer cho một bài báo
        /// </summary>
        /// <param name="paperId">ID của bài báo cần kiểm duyệt</param>
        [HttpGet("{paperId:int}/reviews")]
        public async Task<ActionResult<AdminPaperReviewsSummaryResponse>> GetPaperReviews(int paperId)
        {
            try
            {
                var summary = await _workflowService.GetPaperReviewsSummaryForAdminAsync(paperId);
                return Ok(summary);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin phê duyệt xuất bản chính thức bài báo (trạng thái bài báo chuyển sang PUBLISHED)
        /// </summary>
        [HttpPost("{paperId:int}/publish")]
        public async Task<ActionResult<AdminPaperReviewsSummaryResponse>> PublishPaper(int paperId, [FromBody] AdminPublishPaperRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var summary = await _workflowService.AdminPublishPaperAsync(paperId, adminId, request);
                return Ok(new { message = "Đã phê duyệt xuất bản bài báo thành công trên ARS.", data = summary });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin yêu cầu tác giả chỉnh sửa, bổ sung theo góp ý của các Reviewer (trạng thái chuyển sang REVISION_REQUIRED)
        /// </summary>
        [HttpPost("{paperId:int}/request-revision")]
        public async Task<ActionResult<AdminPaperReviewsSummaryResponse>> RequestRevision(int paperId, [FromBody] AdminRequestRevisionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var adminId = GetCurrentUserId();
                var summary = await _workflowService.AdminRequestRevisionAsync(paperId, adminId, request);
                return Ok(new { message = "Đã gửi yêu cầu chỉnh sửa bài báo tới tác giả thành công.", data = summary });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin quyết định từ chối bài báo (trạng thái chuyển sang REJECTED)
        /// </summary>
        [HttpPost("{paperId:int}/reject")]
        public async Task<ActionResult<AdminPaperReviewsSummaryResponse>> RejectPaper(int paperId, [FromBody] AdminRejectPaperRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var adminId = GetCurrentUserId();
                var summary = await _workflowService.AdminRejectPaperAsync(paperId, adminId, request);
                return Ok(new { message = "Đã quyết định từ chối bài báo thành công.", data = summary });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
