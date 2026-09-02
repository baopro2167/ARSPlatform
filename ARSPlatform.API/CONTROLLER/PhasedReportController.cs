using System;
using System.Collections.Generic;
using System.Security.Claims;
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
    public class PhasedReportController : ControllerBase
    {
        private readonly IPhasedReportService _service;

        public PhasedReportController(IPhasedReportService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách báo cáo tiến độ (có thể lọc theo researchGroupId hoặc topicId)
        /// </summary>
        /// <param name="researchGroupId">ID nhóm nghiên cứu (tùy chọn)</param>
        /// <param name="topicId">ID đề tài nghiên cứu (tùy chọn)</param>
        /// <returns>Danh sách báo cáo tiến độ</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhasedReportResponse>>> GetAll(
            [FromQuery] int? researchGroupId = null,
            [FromQuery] int? topicId = null)
        {
            var items = await _service.GetAllAsync(researchGroupId, topicId);
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách 5 Phase Report theo Đề tài nghiên cứu (ResearchTopic)
        /// </summary>
        /// <param name="topicId">ID đề tài nghiên cứu</param>
        /// <returns>Danh sách các Phase Report thuộc đề tài</returns>
        [HttpGet("topic/{topicId:int}")]
        [HttpGet("by-topic/{topicId:int}")]
        public async Task<ActionResult<IEnumerable<PhasedReportResponse>>> GetByTopic(int topicId)
        {
            var items = await _service.GetByTopicIdAsync(topicId);
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách thành viên nhóm thuộc đề tài nghiên cứu (để hiển thị thành viên & Leader nộp bài)
        /// </summary>
        /// <param name="topicId">ID đề tài nghiên cứu</param>
        /// <returns>Danh sách thành viên nhóm nghiên cứu</returns>
        [HttpGet("topic/{topicId:int}/members")]
        public async Task<ActionResult<IEnumerable<GroupMemberResponse>>> GetMembersByTopic(int topicId)
        {
            var items = await _service.GetMembersByTopicIdAsync(topicId);
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách báo cáo tiến độ theo nhóm nghiên cứu
        /// </summary>
        /// <param name="groupId">ID nhóm nghiên cứu</param>
        /// <returns>Danh sách báo cáo</returns>
        [HttpGet("group/{groupId:int}")]
        public async Task<ActionResult<IEnumerable<PhasedReportResponse>>> GetByGroup(int groupId)
        {
            var items = await _service.GetAllAsync(groupId);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="researchGroupId">ID nhóm nghiên cứu (tùy chọn)</param>
        /// <param name="topicId">ID đề tài nghiên cứu (tùy chọn)</param>
        /// <returns>Danh sách báo cáo tiến độ có phân trang</returns>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<PhasedReportResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? researchGroupId = null,
            [FromQuery] int? topicId = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, researchGroupId, topicId);
            return Ok(result);
        }

        /// <summary>
        /// Giảng viên thiết lập các cột mốc (Phase 1..5) cho Đề tài nghiên cứu (ResearchTopic)
        /// </summary>
        /// <param name="request">Thông tin topicId và danh sách các Phase kèm Deadline</param>
        /// <returns>Danh sách các Phase Report vừa tạo/cập nhật</returns>
        [HttpPost("topic-milestones")]
        public async Task<ActionResult<IEnumerable<PhasedReportResponse>>> CreateTopicMilestones([FromBody] TopicMilestonesCreateRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var response = await _service.CreateTopicMilestonesAsync(request, currentUserId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Trưởng nhóm (Leader) nộp bài báo cáo vào giai đoạn Phase Report (Tự động kiểm tra Deadline &amp; gán trạng thái OnTime / Overdue)
        /// </summary>
        /// <param name="request">Thông tin nộp bài (phasedReportId hoặc topicId + phaseNumber, file url, groupId, memberId)</param>
        /// <returns>Chi tiết Phase Report sau khi nộp</returns>
        [HttpPost("submit")]
        public async Task<ActionResult<PhasedReportResponse>> SubmitReport([FromBody] PhasedReportSubmitRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var response = await _service.SubmitReportAsync(request, currentUserId);
                return Ok(new
                {
                    Message = response.Status == "Overdue" ? "Nộp báo cáo thành công (Trạng thái: Nộp muộn - Overdue)." : "Nộp báo cáo thành công (Trạng thái: Đúng hạn - OnTime).",
                    Data = new
                    {
                        response.PhasedReportId,
                        response.ResearchGroupId,
                        response.TopicId,
                        response.TopicTitle,
                        response.GroupMemberId,
                        response.ReportFileUrl,
                        response.CapacityEvaluation,
                        response.FinalOutcomeEvaluation,
                        response.LectureFeedback,
                        response.LecturerDescription,
                        response.PhaseNumber,
                        response.MilestoneTitle,
                        response.Status,
                        response.CreatedAt,
                        response.DeadlineAt,
                        response.SubmittedAt,
                        response.UpdatedAt,
                        response.GroupName,
                        response.StudentName,
                        response.IsOverdue
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Giảng viên đánh giá, chấm điểm và ghi nhận xét (LecturerDescription) cho Phase Report của nhóm
        /// </summary>
        /// <param name="id">ID báo cáo cần đánh giá</param>
        /// <param name="request">Thông tin đánh giá (nhận xét, điểm, năng lực, kết quả)</param>
        /// <returns>Báo cáo sau khi được đánh giá</returns>
        [HttpPut("{id:int}/evaluate")]
        public async Task<ActionResult<PhasedReportResponse>> EvaluateReport(int id, [FromBody] PhasedReportEvaluationRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var response = await _service.EvaluateReportAsync(id, request, currentUserId);
            if (response == null) return NotFound(new { Message = "Không tìm thấy báo cáo tiến độ." });
            return Ok(new
            {
                Message = "Đã lưu nhận xét và đánh giá báo cáo tiến độ thành công.",
                Data = response
            });
        }

        /// <summary>
        /// Sinh viên / Giảng viên tạo mới một bản ghi báo cáo tiến độ
        /// </summary>
        /// <param name="request">Thông tin báo cáo</param>
        /// <returns>Báo cáo vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<PhasedReportResponse>> Create([FromBody] PhasedReportCreateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var response = await _service.CreateAsync(request, currentUserId);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết báo cáo tiến độ theo ID
        /// </summary>
        /// <param name="id">ID báo cáo</param>
        /// <returns>Chi tiết báo cáo tiến độ</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PhasedReportResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Phased report not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin báo cáo tiến độ
        /// </summary>
        /// <param name="id">ID báo cáo cần cập nhật</param>
        /// <param name="request">Dữ liệu cập nhật</param>
        /// <returns>Báo cáo sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<PhasedReportResponse>> Update(int id, [FromBody] PhasedReportUpdateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var response = await _service.UpdateAsync(id, request, currentUserId);
            if (response == null) return NotFound(new { Message = "Phased report not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một báo cáo tiến độ
        /// </summary>
        /// <param name="id">ID báo cáo cần xóa</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Phased report not found." });
            return Ok(new { Message = "Deleted successfully." });
        }
    }
}
