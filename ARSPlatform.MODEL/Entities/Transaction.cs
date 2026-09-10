namespace ARSPlatform.MODEL.Entities;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public string? Type { get; set; }

    public decimal? Amount { get; set; }

    public string? Status { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    // Payment fields (PayOS/VNPay)
    public string? PaymentOrderId { get; set; }

    public string? PaymentTransactionId { get; set; }

    public string? PaymentResponseCode { get; set; }

    // AnnualFee purchase
    public int? AnnualFeeId { get; set; }

    /// <summary>
    /// Ai thanh toán (FE truyền xuống)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Mô tả: "Lecturer Payment 6 Month"
    /// </summary>
    public string? PaymentDescription { get; set; }

    // Navigation
    public virtual User? User { get; set; }
    public virtual AnnualFee? AnnualFee { get; set; }
}
