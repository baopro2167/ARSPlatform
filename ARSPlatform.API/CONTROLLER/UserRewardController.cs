using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER;

[ApiController]
[Route("api/UserRewards")]
[Authorize]
public class UserRewardController : ControllerBase
{
    private readonly IUserRewardService _service;

    public UserRewardController(IUserRewardService service)
    {
        _service = service;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "0");

    private string[] GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

    private bool IsAdmin() => GetUserRoles().Contains("Admin", StringComparer.OrdinalIgnoreCase);

    // ─────────────────────────────────────────────
    // ADMIN: CRUD
    // ─────────────────────────────────────────────

    /// <summary>
    /// List tất cả UserReward (Admin) — có phân trang + filter
    /// </summary>
    /// GET /api/UserRewards?page=1&pageSize=10&search=researcher&status=Active&sortBy=Name&sortDir=asc
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<UserRewardResponse>>> GetAll([FromQuery] UserRewardFilterParams filter)
    {
        var result = await _service.GetAllPagedAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết 1 UserReward (Admin)
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserRewardResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "UserReward not found." });
        return Ok(result);
    }

    /// <summary>
    /// Tạo UserReward mới (Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserRewardResponse>> Create([FromBody] UserRewardCreateRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật UserReward (Admin)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserRewardResponse>> Update(int id, [FromBody] UserRewardUpdateRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            if (result == null) return NotFound(new { message = "UserReward not found." });
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Toggle bật/tắt UserReward (Admin)
    /// </summary>
    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserRewardResponse>> Toggle(int id, [FromBody] UserRewardToggleRequest request)
    {
        var result = await _service.ToggleAsync(id, request.IsActive);
        if (result == null) return NotFound(new { message = "UserReward not found." });
        return Ok(result);
    }

    /// <summary>
    /// Xóa UserReward (Admin)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound(new { message = "UserReward not found." });
        return NoContent();
    }
}

/// <summary>
/// Toggle status (Admin)
/// </summary>
public class UserRewardToggleRequest
{
    public bool IsActive { get; set; }
}
