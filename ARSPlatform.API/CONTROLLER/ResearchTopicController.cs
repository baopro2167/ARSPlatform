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
    public class ResearchTopicController : ControllerBase
    {
        private readonly IResearchTopicService _service;

        public ResearchTopicController(IResearchTopicService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách đề tài nghiên cứu (có thể lọc theo lecturerId)
        /// </summary>
        /// <param name="lecturerId">ID Giảng viên phụ trách (tùy chọn)</param>
        /// <returns>Danh sách đề tài nghiên cứu</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ResearchTopicResponse>>> GetAll([FromQuery] int? lecturerId = null)
        {
            var items = await _service.GetAllAsync(lecturerId);
            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách đề tài nghiên cứu của Giảng viên hiện tại
        /// </summary>
        /// <returns>Danh sách đề tài nghiên cứu của tôi</returns>
        [HttpGet("my-topics")]
        public async Task<ActionResult<IEnumerable<ResearchTopicResponse>>> GetMyTopics()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var items = await _service.GetMyTopicsAsync(currentUserId.Value);
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <param name="lecturerId">ID Giảng viên phụ trách (tùy chọn)</param>
        /// <returns>Danh sách đề tài nghiên cứu có phân trang</returns>
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ResearchTopicResponse>>> GetPaged(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? lecturerId = null)
        {
            var result = await _service.GetPagedAsync(paginationParams, lecturerId);
            return Ok(result);
        }

        /// <summary>
        /// Tạo đề tài nghiên cứu mới (tự động gán LecturerId của tài khoản hiện tại)
        /// </summary>
        /// <param name="request">Thông tin đề tài nghiên cứu</param>
        /// <returns>Đề tài nghiên cứu vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<ResearchTopicResponse>> Create([FromBody] ResearchTopicCreateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var response = await _service.CreateAsync(request, currentUserId);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết đề tài nghiên cứu theo ID
        /// </summary>
        /// <param name="id">ID đề tài nghiên cứu</param>
        /// <returns>Chi tiết đề tài nghiên cứu</returns>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResearchTopicResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Research topic not found." });
            return Ok(item);
        }

        /// <summary>
        /// Cập nhật thông tin đề tài nghiên cứu
        /// </summary>
        /// <param name="id">ID đề tài nghiên cứu</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Đề tài sau khi cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ResearchTopicResponse>> Update(int id, [FromBody] ResearchTopicUpdateRequest request)
        {
            var response = await _service.UpdateAsync(id, request);
            if (response == null) return NotFound(new { Message = "Research topic not found." });
            return Ok(response);
        }

        /// <summary>
        /// Xóa một đề tài nghiên cứu
        /// </summary>
        /// <param name="id">ID đề tài nghiên cứu</param>
        /// <returns>Thông báo kết quả xóa</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { Message = "Research topic not found." });
            return Ok(new { Message = "Deleted successfully." });
        }

        /// <summary>
        /// Lấy danh sách tài liệu học tập gắn với đề tài nghiên cứu
        /// </summary>
        /// <param name="topicId">ID đề tài nghiên cứu</param>
        /// <returns>Danh sách tài liệu học tập</returns>
        [HttpGet("{topicId:int}/learning-materials")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<LearningMaterialResponse>>> GetLearningMaterials(int topicId)
        {
            try
            {
                var materials = await _service.GetLearningMaterialsByTopicIdAsync(topicId);
                return Ok(materials);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gán tài liệu học tập có sẵn từ thư viện vào đề tài nghiên cứu
        /// </summary>
        /// <param name="topicId">ID đề tài nghiên cứu</param>
        /// <param name="request">ID tài liệu học tập cần gán</param>
        /// <returns>Kết quả gán tài liệu</returns>
        [HttpPost("{topicId:int}/learning-materials")]
        public async Task<IActionResult> AssignLearningMaterial(int topicId, [FromBody] AssignTopicLearningMaterialRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                await _service.AssignLearningMaterialAsync(topicId, request.LearningMaterialId, currentUserId.Value);
                return Ok(new { Message = "Learning material assigned to topic successfully." });
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
        }

        /// <summary>
        /// Tạo mới tài liệu học tập và gắn trực tiếp vào đề tài nghiên cứu trong 1 Transaction
        /// </summary>
        /// <param name="topicId">ID đề tài nghiên cứu</param>
        /// <param name="request">Thông tin tài liệu học tập</param>
        /// <returns>Tài liệu vừa tạo</returns>
        [HttpPost("{topicId:int}/learning-materials/create")]
        public async Task<ActionResult<LearningMaterialResponse>> CreateAndAssignLearningMaterial(int topicId, [FromBody] TopicLearningMaterialCreateRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var created = await _service.CreateAndAssignLearningMaterialAsync(topicId, request, currentUserId.Value);
                return StatusCode(201, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gỡ tài liệu học tập khỏi đề tài nghiên cứu (không xóa tài liệu gốc)
        /// </summary>
        /// <param name="topicId">ID đề tài nghiên cứu</param>
        /// <param name="learningMaterialId">ID tài liệu học tập</param>
        /// <returns>Thông báo kết quả gỡ</returns>
        [HttpDelete("{topicId:int}/learning-materials/{learningMaterialId:int}")]
        public async Task<IActionResult> RemoveLearningMaterial(int topicId, int learningMaterialId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                await _service.RemoveLearningMaterialFromTopicAsync(topicId, learningMaterialId, currentUserId.Value);
                return Ok(new { Message = "Learning material removed from topic successfully." });
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
    }
}
