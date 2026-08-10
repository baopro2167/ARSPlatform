namespace ARSPlatform.SERVICE.DTOs.Response;

public class PaymentResponse
{
    public string CheckoutUrl { get; set; }
    public string OrderCode { get; set; }
    public string PaymentLinkId { get; set; }
}
