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
    /// UserId thanh toán — FE truyền xuống
    /// </summary>
    public int? UserId { get; set; }

    public string ReturnUrl { get; set; } = null!;
    public string CancelUrl { get; set; } = null!;
}

/// <summary>
/// PayOS webhook payload cho AnnualFee
/// </summary>
public class AnnualFeePayOSWebhookRequest
{
    public string? OrderCode { get; set; }
    public int? Amount { get; set; }
    public string? Status { get; set; }
    public string? Code { get; set; }
    public string? Desc { get; set; }
    public string? OrderId { get; set; }
    public DateTime? CreatedAt { get; set; }
}
