using System;

namespace ARSPlatform.SERVICE.DTOs.Response;

/// <summary>
/// UserReward response
/// </summary>
public class UserRewardResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int RewardMonths { get; set; }
    public DateTime UpdateAt { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
