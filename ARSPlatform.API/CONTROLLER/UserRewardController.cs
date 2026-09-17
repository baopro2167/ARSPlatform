using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    /// <summary>
    /// CRUD + phân trang cho bảng UserRewards (phần thưởng Researcher).
    /// Áp dụng cơ chế Auto-Match Name khi publish paper: tên FE truyền xuống
    /// (ví dụ "Research Publication Reward") sẽ được server Contains (case-insensitive)
    /// với Name trong DB và bắt buộc Status = "Active".
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserRewardController : ControllerBase
    {
        private readonly IUserRewardService _service;

        public UserRewardController(IUserRewardService service)
        {
            _service = service;
        }

        private int? GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var v) ? v : null;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        /// <summary>
        /// Lấy danh sách UserReward có phân trang + filter (Status, Search).
        /// </summary>
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<ActionResult> GetPaged([FromQuery] UserRewardPaginationRequest request)
        {
            var result = await _service.GetPagedAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Lấy tất cả UserReward (không phân trang) — chỉ Admin.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetAll()
        {
            var result = await _service.GetPagedAsync(new UserRewardPaginationRequest
            {
                PageNumber = 1,
                PageSize = 50
            });
            return Ok(result);
        }

        /// <summary>
        /// Lấy 1 UserReward theo Id.
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { message = "UserReward not found." });
            return Ok(item);
        }

        /// <summary>
        /// Tạo mới UserReward — chỉ Admin.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create([FromBody] UserRewardCreateRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Cập nhật UserReward — chỉ Admin.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Update(int id, [FromBody] UserRewardUpdateRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var updated = await _service.UpdateAsync(id, request);
            if (updated == null) return NotFound(new { message = "UserReward not found." });
            return Ok(updated);
        }

        /// <summary>
        /// PATCH trạng thái nhanh: Active | InActive — chỉ Admin.
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] StatusUpdateBody body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Status))
                return BadRequest(new { message = "Status is required." });

            var updated = await _service.UpdateStatusAsync(id, body.Status);
            if (updated == null) return NotFound(new { message = "UserReward not found." });
            return Ok(updated);
        }

        /// <summary>
        /// Xóa UserReward — chỉ Admin.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound(new { message = "UserReward not found." });
            return NoContent();
        }

        /// <summary>
        /// [Internal / Dev] Auto-match reward theo Name + Status=Active.
        /// Trả về null nếu không tìm thấy reward Active phù hợp.
        /// </summary>
        [HttpPost("match")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> MatchByName([FromBody] MatchRewardRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Name))
                return BadRequest(new { message = "Name is required." });

            var found = await _service.FindActiveByNameContainsAsync(body.Name);
            if (found == null)
                return Ok(new { matched = false, message = "No active reward matched." });
            return Ok(new { matched = true, reward = found });
        }

        public class StatusUpdateBody
        {
            public string Status { get; set; } = null!;
        }

        public class MatchRewardRequest
        {
            public string Name { get; set; } = null!;
        }
    }
}
