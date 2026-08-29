using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentLink(PaymentCreateRequest request);
    Task<PaymentCallbackResponse> ProcessCallback(string orderCode, string status);
    Task<bool> CancelPayment(string orderCode);
}
