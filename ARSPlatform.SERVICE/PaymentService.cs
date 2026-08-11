using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using Microsoft.Extensions.Options;
using static ARSPlatform.SERVICE.PayOSSettings;

namespace ARSPlatform.SERVICE;

public class PaymentService : IPaymentService
{
    private readonly PayOSSettings _payOSSettings;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly HttpClient _httpClient;

    public PaymentService(
        IOptions<PayOSSettings> payOSSettings,
        ITransactionRepository transactionRepository,
        IWalletRepository walletRepository,
        HttpClient httpClient)
    {
        _payOSSettings = payOSSettings.Value;
        _transactionRepository = transactionRepository;
        _walletRepository = walletRepository;
        _httpClient = httpClient;
    }

    public async Task<PaymentResponse> CreatePaymentLink(PaymentCreateRequest request)
    {
        // Generate unique order code
        var orderCode = GenerateOrderCode();
        
        // Convert amount to int (PayOS requires int in VND)
        var amount = (int)(request.Amount * 100); // Amount in VND (smallest unit)
        
        // Create transaction record first
        var transaction = new Transaction
        {
            WalletId = request.WalletId,
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

        var description = request.Description ?? $"Thanh toan don hang {orderCode}";

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

        // Update transaction based on status
        if (status == "PAID" || status == "SUCCESS")
        {
            transaction.Status = "SUCCESS";
            transaction.PaymentResponseCode = "00"; // PayOS success code
            
            // Update wallet balance
            if (transaction.WalletId.HasValue && transaction.Amount.HasValue)
            {
                var wallet = await _walletRepository.GetByIdAsync(transaction.WalletId.Value);
                if (wallet != null)
                {
                    wallet.Balance += transaction.Amount.Value;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _walletRepository.Update(wallet);
                    await _walletRepository.SaveChangesAsync();
                }
            }
        }
        else
        {
            transaction.Status = "FAILED";
            transaction.PaymentResponseCode = status;
        }

        _transactionRepository.Update(transaction);
        await _transactionRepository.SaveChangesAsync();

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

            // PayOS requires API key in Authorization header as Bearer token
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Client-Id", _payOSSettings.ClientId);
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _payOSSettings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_payOSSettings.ApiKey}");

            var response = await _httpClient.PostAsync(apiUrl, content);
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
