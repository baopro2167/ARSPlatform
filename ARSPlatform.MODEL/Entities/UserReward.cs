namespace ARSPlatform.MODEL.Entities;

/// <summary>
/// Bảng cấu hình thưởng/thành tích cho User - admin có thể thay đổi tùy ý
/// </summary>
public class UserReward
{
    public int Id { get; set; }

    /// <summary>
    /// Tên cấu hình (VD: "Gói cộng thêm 1 tháng", "Gói cộng thêm 6 tháng")
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Mô tả chi tiết (nullable)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Số THÁNG thưởng mà Researcher được cộng thêm vào ExpiresAt khi publish bài báo
    /// (VD: 1 = 1 tháng, 2 = 2 tháng, 3 = 3 tháng...)
    /// </summary>
    public int RewardMonths { get; set; }

    /// <summary>
    /// Ngày cập nhật/thay đổi (admin có thể sửa tùy ý)
    /// </summary>
    public DateTime UpdateAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Trạng thái: Active | InActive
    /// </summary>
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
