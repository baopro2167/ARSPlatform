using System;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request;

/// <summary>
/// Tạo cấu hình UserReward (Admin)
/// </summary>
public class UserRewardCreateRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int RewardMonths { get; set; }

    [Required]
    public DateTime UpdateAt { get; set; }

    [Required]
    public string Status { get; set; } = "Active"; // Active | InActive
}

/// <summary>
/// Cập nhật cấu hình UserReward (Admin)
/// </summary>
public class UserRewardUpdateRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int RewardMonths { get; set; }

    [Required]
    public DateTime UpdateAt { get; set; }

    [Required]
    public string Status { get; set; } // Active | InActive
}

/// <summary>
/// Filter/pagination cho UserReward (Admin)
/// </summary>
public class UserRewardFilterParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDir { get; set; } = "desc";
}
