namespace ARSPlatform.MODEL.Entities;

public class UserSubscription
{
    public int Id { get; set; }

    /// <summary>
    /// User sở hữu subscription này
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Vai trò: Researcher | Lecturer
    /// Mỗi user × role chỉ có 1 dòng
    /// </summary>
    public string UserRole { get; set; } = null!;

    /// <summary>
    /// Ngày hết hạn subscription (NULL = chưa có subscription)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Transaction mua gói gần nhất
    /// </summary>
    public int? LatestTransactionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual User? User { get; set; }
    public virtual Transaction? LatestTransaction { get; set; }
}
