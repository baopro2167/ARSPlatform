using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER;

[ApiController]
[Route("api/AnnualFees")]
[Authorize]
public class AnnualFeeController : ControllerBase
{
    private readonly IAnnualFeeService _service;

    public AnnualFeeController(IAnnualFeeService service)
    {
        _service = service;
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────
    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "0");

    private string GetUserRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value
        ?? User.FindFirst("role")?.Value
        ?? "";

    private string[] GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

    private bool IsAdmin() => GetUserRoles().Contains("Admin", StringComparer.OrdinalIgnoreCase);

    // ─────────────────────────────────────────────
    // ADMIN: CRUD
    // ─────────────────────────────────────────────

    /// <summary>
    /// List tất cả gói AnnualFee (Admin) — có phân trang + filter
    /// </summary>
    /// GET /api/AnnualFees?page=1&pageSize=10&search=researcher&userRole=Lecturer&billingCycle=SixMonth&status=true&sortBy=Price&sortDir=asc
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<AnnualFeeResponse>>> GetAll([FromQuery] AnnualFeeFilterParams filter)
    {
        var result = await _service.GetAllPagedAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết 1 gói (Admin)
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AnnualFeeResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "AnnualFee not found." });
        return Ok(result);
    }

    /// <summary>
    /// Tạo gói AnnualFee mới (Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AnnualFeeResponse>> Create([FromBody] AnnualFeeCreateRequest request)
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật gói AnnualFee (Admin)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AnnualFeeResponse>> Update(int id, [FromBody] AnnualFeeUpdateRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            if (result == null) return NotFound(new { message = "AnnualFee not found." });
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Toggle bật/tắt gói AnnualFee (Admin)
    /// </summary>
    [HttpPatch("{id:int}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AnnualFeeResponse>> Toggle(int id, [FromBody] AnnualFeeToggleRequest request)
    {
        try
        {
            var result = await _service.ToggleAsync(id, request);
            if (result == null) return NotFound(new { message = "AnnualFee not found." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa gói AnnualFee (Admin) — refuse nếu có transaction
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { message = "AnnualFee not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ─────────────────────────────────────────────
    // PUBLIC: Active plans
    // ─────────────────────────────────────────────

    /// <summary>
    /// List gói đang bật (Auth — FE Subscription tab).
    /// Tự động filter theo role của user:
    ///   - Researcher → chỉ thấy plan Researcher
    ///   - Lecturer   → chỉ thấy plan Lecturer
    ///   - Role khác (Guest/Admin/Reviewer/Graduate Student) → trả rỗng
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<PagedResult<AnnualFeeResponse>>> GetActive(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var role = GetUserRole();
        var result = await _service.GetActiveForRoleAsync(role, page, pageSize);
        return Ok(result);
    }

    // ─────────────────────────────────────────────
    // USER: Subscription
    // ─────────────────────────────────────────────

    /// <summary>
    /// Subscription hiện tại của user (header badge + subscription tab)
    /// </summary>
    [HttpGet("my-subscription")]
    public async Task<ActionResult<MySubscriptionResponse>> GetMySubscription()
    {
        var userId = GetUserId();
        var role = GetUserRole();

        if (string.IsNullOrEmpty(role))
            return Unauthorized(new { message = "Role not found in token." });

        var result = await _service.GetMySubscriptionAsync(userId, role);
        if (result == null)
            return Ok(new { message = "No active subscription." });

        return Ok(result);
    }

    /// <summary>
    /// Purchase history của user
    /// </summary>
    [HttpGet("my-purchases")]
    public async Task<ActionResult<PagedResult<AnnualFeePurchaseResponse>>> GetMyPurchases(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var result = await _service.GetMyPurchasesAsync(userId, role, page, pageSize);
        return Ok(result);
    }

    // ─────────────────────────────────────────────
    // USER: Purchase
    // ─────────────────────────────────────────────

    /// <summary>
    /// Mua gói AnnualFee — trả về PayOS checkoutUrl (Researcher / Lecturer)
    /// </summary>
    [HttpPost("{id:int}/purchase")]
    [Authorize(Roles = "Researcher,Lecturer")]
    public async Task<ActionResult<AnnualFeePurchaseResultResponse>> Purchase(
        int id, [FromBody] AnnualFeePurchaseRequest request)
    {
        try
        {
            var role = GetUserRole();
            var result = await _service.PurchaseAsync(id, request, role);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─────────────────────────────────────────────
    // PAYOS WEBHOOK
    // ─────────────────────────────────────────────

    /// <summary>
    /// PayOS server-to-server webhook — verify HMAC signature
    /// </summary>
    [HttpPost("payos-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOSWebhook(
        [FromBody] AnnualFeePayOSWebhookRequest webhook)
    {
        // 1. Get raw body for signature verification
        var rawBody = Request.Headers["X-Raw-Body"].FirstOrDefault();
        if (string.IsNullOrEmpty(rawBody))
        {
            // Fallback: serialize the webhook object
            rawBody = System.Text.Json.JsonSerializer.Serialize(webhook);
        }

        // Signature from PayOS header
        var signature = Request.Headers["X-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
            return Unauthorized(new { message = "Missing PayOS webhook signature." });

        // 2. Compute expected signature
        var settings = HttpContext.RequestServices
            .GetService<Microsoft.Extensions.Options.IOptions<PayOSSettings>>()
            ?.Value;

        if (settings == null || string.IsNullOrEmpty(settings.ChecksumKey))
            return StatusCode(500, new { message = "PayOS not configured." });

        var expectedSig = ComputeHmacSha256(rawBody, settings.ChecksumKey);
        if (signature != expectedSig)
            return Unauthorized(new { message = "Invalid PayOS webhook signature." });

        // 3. Process webhook
        var success = await _service.ProcessPayOSWebhookAsync(webhook);

        return Ok(new { ok = true });
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}
