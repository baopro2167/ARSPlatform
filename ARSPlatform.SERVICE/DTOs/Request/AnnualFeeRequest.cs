using System;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request;

/// <summary>
/// Tạo gói AnnualFee (Admin)
/// </summary>
public class AnnualFeeCreateRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [Required]
    public string UserRole { get; set; } = null!; // Researcher | Lecturer

    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required]
    public string BillingCycle { get; set; } = null!; // SixMonth | Annual

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool Status { get; set; } = true;
}

/// <summary>
/// Cập nhật gói AnnualFee (Admin)
/// </summary>
public class AnnualFeeUpdateRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [Required]
    public string UserRole { get; set; } = null!;

    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required]
    public string BillingCycle { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Toggle active (Admin)
/// </summary>
public class AnnualFeeToggleRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Filter/pagination cho AnnualFees (Admin)
/// </summary>
public class AnnualFeeFilterParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? UserRole { get; set; }
    public string? BillingCycle { get; set; }
    public bool? Status { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDir { get; set; } = "desc";
}

/// <summary>
/// Mua AnnualFee (User)
/// </summary>
public class AnnualFeePurchaseRequest
{
    /// <summary>
    /// UserId thanh toán — FE truyền xuống hoặc tự động lấy từ JWT
    /// </summary>
    public int? UserId { get; set; }

    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}

/// <summary>
/// PayOS webhook payload cho AnnualFee (hỗ trợ cả chuẩn PayOS và định dạng trực tiếp)
/// </summary>
public class AnnualFeePayOSWebhookRequest
{
    public string? Code { get; set; }
    public string? Desc { get; set; }
    public PayOSWebhookDataDto? Data { get; set; }
    public string? Signature { get; set; }

    // Fallback các trường trực tiếp
    public string? OrderCode { get; set; }
    public int? Amount { get; set; }
    public string? Status { get; set; }
    public string? OrderId { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class PayOSWebhookDataDto
{
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public string? TransactionDateTime { get; set; }
    public string? Currency { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? Code { get; set; }
    public string? Desc { get; set; }
}

/// <summary>
/// Tham số lọc + phân trang cho GET /api/AnnualFees/admin/subscriptions
/// </summary>
public class AdminSubscriptionListParams
{
    /// <summary>Số trang, bắt đầu từ 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Số item mỗi trang. Tối đa 100.</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>Tìm kiếm theo FullName hoặc Email của User.</summary>
    public string? Search { get; set; }

    /// <summary>Lọc theo Role người dùng (Researcher, Lecturer, ...).</summary>
    public string? Role { get; set; }

    /// <summary>
    /// Lọc theo trạng thái subscription:
    ///   Active  — ExpiresAt > now
    ///   Expired — ExpiresAt != null &amp;&amp; ExpiresAt &lt;= now
    ///   None    — không có record UserSubscription (hoặc ExpiresAt == null)
    /// </summary>
    public string? Status { get; set; }
}

