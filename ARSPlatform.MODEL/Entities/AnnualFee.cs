namespace ARSPlatform.MODEL.Entities;

public class AnnualFee
{
    public int Id { get; set; }

    /// <summary>
    /// Tên gói (VD: "Researcher Annual Fee")
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Vai trò áp dụng: Researcher | Lecturer
    /// </summary>
    public string UserRole { get; set; } = null!;

    /// <summary>
    /// Giá VND (decimal, không có .00 phụ)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Chu kỳ: SixMonth | Annual
    /// </summary>
    public string BillingCycle { get; set; } = null!;

    /// <summary>
    /// Ngày bắt đầu hiệu lực
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Ngày kết thúc (NULL = không giới hạn)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Bật = 1 (mua được), Tắt = 0
    /// </summary>
    public bool Status { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
