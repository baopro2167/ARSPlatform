namespace ARSPlatform.SERVICE.DTOs.Request;

public class PaymentCreateRequest
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public int? UserId { get; set; }
    public int? WalletId { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}
