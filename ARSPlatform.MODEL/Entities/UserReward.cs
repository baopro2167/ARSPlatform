using System;

namespace ARSPlatform.MODEL.Entities;

/// <summary>
/// Cấu hình phần thưởng cho Researcher (cộng tháng vào subscription).
/// Khi Researcher publish bài báo thành công, hệ thống sẽ lookup reward
/// theo Name (contains, case-insensitive) và Status = "Active", lấy RewardMonths
/// cộng vào UserSubscriptions.ExpiresAt với UserRole = "Researcher".
/// </summary>
public class UserReward
{
    public int Id { get; set; }

    /// <summary>
    /// Tên phần thưởng, ví dụ: "Research Publication Reward".
    /// FE sẽ truyền xuống tên dạng Title Case có space; server so khớp
    /// bằng Contains (case-insensitive) để chấp nhận mọi biến thể
    /// (research_publication_reward, RESEARCH_PUBLICATION_REWARD,
    /// Research-Publication-Reward, ...).
    /// </summary>
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Số tháng thưởng sẽ được cộng thêm vào ExpiresAt khi điều kiện kích hoạt xảy ra.
    /// </summary>
    public int RewardMonths { get; set; }

    public DateTime UpdateAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Active | InActive. Chỉ reward Status = "Active" mới được dùng để cộng tháng.
    /// </summary>
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
