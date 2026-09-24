using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using Microsoft.Extensions.Options;
using ARSPlatform.SERVICE.Interfaces;
using static ARSPlatform.SERVICE.PayOSSettings;

namespace ARSPlatform.SERVICE;

public class PaymentService : IPaymentService
{
    private readonly PayOSSettings _payOSSettings;
    private readonly ITransactionRepository _transactionRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly HttpClient _httpClient;
    private readonly IAnnualFeeService _annualFeeService;

    public PaymentService(
        IOptions<PayOSSettings> payOSSettings,
        ITransactionRepository transactionRepository,
        INotificationRepository notificationRepository,
        HttpClient httpClient,
        IAnnualFeeService annualFeeService)
    {
        _payOSSettings = payOSSettings.Value;
        _transactionRepository = transactionRepository;
        _notificationRepository = notificationRepository;
        _httpClient = httpClient;
        _annualFeeService = annualFeeService;
    }

    public async Task<PaymentResponse> CreatePaymentLink(PaymentCreateRequest request)
    {
        // Generate unique order code
        var orderCode = GenerateOrderCode();
        
        // Convert amount to int (PayOS requires int in VND)
        var amount = (int)request.Amount; // Amount in VND
        
        // Create transaction record first
        var transaction = new Transaction
        {
            Type = "PAYOS",
            Amount = request.Amount,
            Status = "PENDING",
            Description = request.Description,
            PaymentOrderId = orderCode.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        
        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        // Build return URLs
        var returnUrl = !string.IsNullOrEmpty(request.ReturnUrl) 
            ? request.ReturnUrl 
            : _payOSSettings.ReturnUrl;
        
        var cancelUrl = !string.IsNullOrEmpty(request.CancelUrl) 
            ? request.CancelUrl 
            : _payOSSettings.CancelUrl;

        var rawDesc = !string.IsNullOrWhiteSpace(request.Description) 
            ? request.Description 
            : $"Don hang {orderCode}";
        var description = rawDesc.Length > 25 ? rawDesc[..25] : rawDesc;

        // Create signature (Keys must be in alphabetical order: amount, cancelUrl, description, orderCode, returnUrl)
        var signatureData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
        var signature = ComputeHmacSha256(signatureData, _payOSSettings.ChecksumKey);

        // Prepare request body
        var requestBody = new Dictionary<string, object>
        {
            { "orderCode", orderCode },
            { "amount", amount },
            { "description", description },
            { "returnUrl", returnUrl },
            { "cancelUrl", cancelUrl },
            { "signature", signature }
        };

        // Call PayOS API
        var response = await CallPayOSApi(requestBody);

        if (response != null && response.ContainsKey("checkoutUrl"))
        {
            return new PaymentResponse
            {
                CheckoutUrl = response["checkoutUrl"].ToString(),
                OrderCode = orderCode.ToString(),
                PaymentLinkId = response.ContainsKey("id") ? response["id"].ToString() : ""
            };
        }

        throw new Exception("Failed to create payment link");
    }

    public async Task<PaymentCallbackResponse> ProcessCallback(string orderCode, string status)
    {
        // Find transaction by order code
        var transaction = await _transactionRepository.GetByOrderCodeAsync(orderCode);
        
        if (transaction == null)
        {
            return new PaymentCallbackResponse
            {
                Success = false,
                Message = "Transaction not found"
            };
        }

        // If the transaction has already been processed successfully, check if AnnualFee subscription needs activation
        if (transaction.Status == "SUCCESS" || transaction.Status == "ACTIVE")
        {
            if (transaction.Type == "ANNUAL_FEE" || transaction.AnnualFeeId.HasValue)
            {
                try
                {
                    await _annualFeeService.ProcessPayOSWebhookAsync(new AnnualFeePayOSWebhookRequest
                    {
                        OrderCode = orderCode,
                        Status = status,
                        Code = "00",
                        Data = new PayOSWebhookDataDto
                        {
                            OrderCode = long.TryParse(orderCode, out var oc) ? oc : 0,
                            Code = "00"
                        }
                    });
                }
                catch
                {
                }
            }

            return new PaymentCallbackResponse
            {
                Success = true,
                Message = "Transaction already processed successfully",
                OrderCode = orderCode,
                RedirectUrl = $"{_payOSSettings.ReturnUrl}?success=true&orderCode={orderCode}"
            };
        }

        // Update transaction based on status
        if (status == "PAID" || status == "SUCCESS")
        {
            transaction.Status = "ACTIVE";
            transaction.PaymentResponseCode = "00"; // PayOS success code
        }
        else
        {
            transaction.Status = "FAILED";
            transaction.PaymentResponseCode = status;
        }

        _transactionRepository.Update(transaction);
        await _transactionRepository.SaveChangesAsync();

        // Kích hoạt gói AnnualFee nếu giao dịch là mua gói thường niên
        if ((status == "PAID" || status == "SUCCESS") && (transaction.Type == "ANNUAL_FEE" || transaction.AnnualFeeId.HasValue))
        {
            try
            {
                await _annualFeeService.ProcessPayOSWebhookAsync(new AnnualFeePayOSWebhookRequest
                {
                    OrderCode = orderCode,
                    Status = status,
                    Code = "00",
                    Data = new PayOSWebhookDataDto
                    {
                        OrderCode = long.TryParse(orderCode, out var oc) ? oc : 0,
                        Code = "00"
                    }
                });
            }
            catch
            {
            }
        }

        if ((status == "PAID" || status == "SUCCESS") && transaction.UserId.HasValue && transaction.UserId.Value > 0)
        {
            try
            {
                var desc = !string.IsNullOrWhiteSpace(transaction.Description) ? transaction.Description : $"Đơn hàng {orderCode}";
                var notif = new Notification
                {
                    UserId = transaction.UserId.Value,
                    Message = $"[Thanh toán] Giao dịch \"{desc}\" với số tiền {transaction.Amount:N0} VNĐ đã được xử lý thành công.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationRepository.AddAsync(notif);
                await _notificationRepository.SaveChangesAsync();
            }
            catch
            {
                // Ignore notification error
            }
        }

        // Redirect URL based on status
        var redirectUrl = status == "PAID" || status == "SUCCESS"
            ? $"{_payOSSettings.ReturnUrl}?success=true&orderCode={orderCode}"
            : $"{_payOSSettings.CancelUrl}?success=false&orderCode={orderCode}";

        return new PaymentCallbackResponse
        {
            Success = status == "PAID" || status == "SUCCESS",
            Message = status == "PAID" || status == "SUCCESS" ? "Payment successful" : "Payment failed",
            OrderCode = orderCode,
            RedirectUrl = redirectUrl
        };
    }

    public async Task<bool> CancelPayment(string orderCode)
    {
        var transaction = await _transactionRepository.GetByOrderCodeAsync(orderCode);
        
        if (transaction == null)
            return false;

        transaction.Status = "CANCELLED";
        _transactionRepository.Update(transaction);
        await _transactionRepository.SaveChangesAsync();

        return true;
    }

    private long GenerateOrderCode()
    {
        // Generate unique order code based on timestamp + random
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100000000;
    }

    private string ComputeHmacSha256(string data, string key)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }
    }

    private async Task<Dictionary<string, object>?> CallPayOSApi(Dictionary<string, object> requestBody)
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // PayOS API endpoint
            var apiUrl = $"{_payOSSettings.BaseUrl}/v2/payment-requests";

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = content
            };
            requestMessage.Headers.Add("X-Client-Id", _payOSSettings.ClientId);
            requestMessage.Headers.Add("X-Api-Key", _payOSSettings.ApiKey);

            var response = await _httpClient.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                var code = root.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : "";
                var desc = root.TryGetProperty("desc", out var descProp) ? descProp.GetString() : "Unknown error";

                if (code != "00")
                {
                    throw new Exception($"PayOS error code {code}: {desc}");
                }

                if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
                {
                    var result = new Dictionary<string, object>();
                    foreach (var prop in dataElement.EnumerateObject())
                    {
                        result[prop.Name] = prop.Value;
                    }
                    return result;
                }
            }
            else
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;
                    var desc = root.TryGetProperty("desc", out var descProp) ? descProp.GetString() : response.ReasonPhrase;
                    throw new Exception(desc);
                }
                catch
                {
                    throw new Exception($"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            throw new Exception($"PayOS API error: {ex.Message}");
        }
    }
}
