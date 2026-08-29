namespace ARSPlatform.SERVICE.DTOs.Response;

public class PaymentCallbackResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? OrderCode { get; set; }
    public string? RedirectUrl { get; set; }
}
