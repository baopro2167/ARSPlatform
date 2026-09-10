using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ARSPlatform.SERVICE;

public class AnnualFeeService : IAnnualFeeService
{
    private readonly IAnnualFeeRepository _annualFeeRepo;
    private readonly IUserSubscriptionRepository _subscriptionRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly PayOSSettings _payOSSettings;
    private readonly HttpClient _httpClient;

    public AnnualFeeService(
        IAnnualFeeRepository annualFeeRepo,
        IUserSubscriptionRepository subscriptionRepo,
        ITransactionRepository transactionRepo,
        IOptions<PayOSSettings> payOSSettings,
        HttpClient httpClient)
    {
        _annualFeeRepo = annualFeeRepo;
        _subscriptionRepo = subscriptionRepo;
        _transactionRepo = transactionRepo;
        _payOSSettings = payOSSettings.Value;
        _httpClient = httpClient;
    }

    // ─────────────────────────────────────────────
    // ADMIN CRUD
    // ─────────────────────────────────────────────

    public async Task<PagedResult<AnnualFeeResponse>> GetAllPagedAsync(AnnualFeeFilterParams filter)
    {
        var page = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var size = filter.PageSize < 1 ? 10 : filter.PageSize;

        var query = _annualFeeRepo.GetQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(a => a.Name.Contains(filter.Search));

        if (!string.IsNullOrWhiteSpace(filter.UserRole))
            query = query.Where(a => a.UserRole == filter.UserRole);

        if (!string.IsNullOrWhiteSpace(filter.BillingCycle))
            query = query.Where(a => a.BillingCycle == filter.BillingCycle);

        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);

        // Sort
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDir == "asc"
                ? query.OrderBy(a => a.Name)
                : query.OrderByDescending(a => a.Name),
            "price" => filter.SortDir == "asc"
                ? query.OrderBy(a => a.Price)
                : query.OrderByDescending(a => a.Price),
            "createdat" => filter.SortDir == "asc"
                ? query.OrderBy(a => a.CreatedAt)
                : query.OrderByDescending(a => a.CreatedAt),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PagedResult<AnnualFeeResponse>(
            items.Select(MapToResponse).ToList(),
            total, page, size);
    }

    public async Task<AnnualFeeResponse?> GetByIdAsync(int id)
    {
        var entity = await _annualFeeRepo.GetByIdAsync(id);
        return entity == null ? null : MapToResponse(entity);
    }

    public async Task<AnnualFeeResponse?> GetActiveByIdAsync(int id)
    {
        var entity = await _annualFeeRepo.GetByIdAsync(id);
        if (entity == null || !entity.Status) return null;
        return MapToResponse(entity);
    }

    public async Task<AnnualFeeResponse> CreateAsync(AnnualFeeCreateRequest request)
    {
        // Validate BillingCycle
        if (request.BillingCycle != "SixMonth" && request.BillingCycle != "Annual")
            throw new ArgumentException("BillingCycle must be 'SixMonth' or 'Annual'");

        // Validate UserRole
        if (request.UserRole != "Researcher" && request.UserRole != "Lecturer")
            throw new ArgumentException("UserRole must be 'Researcher' or 'Lecturer'");

        // Check duplicate active
        if (request.Status && await _annualFeeRepo.ExistsActiveDuplicateAsync(
                request.UserRole, request.BillingCycle))
            throw new InvalidOperationException(
                $"An active plan already exists for {request.UserRole} × {request.BillingCycle}");

        var entity = new AnnualFee
        {
            Name = request.Name,
            UserRole = request.UserRole,
            Price = request.Price,
            BillingCycle = request.BillingCycle,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _annualFeeRepo.AddAsync(entity);
        await _annualFeeRepo.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<AnnualFeeResponse?> UpdateAsync(int id, AnnualFeeUpdateRequest request)
    {
        var entity = await _annualFeeRepo.GetByIdAsync(id);
        if (entity == null) return null;

        // Validate
        if (request.BillingCycle != "SixMonth" && request.BillingCycle != "Annual")
            throw new ArgumentException("BillingCycle must be 'SixMonth' or 'Annual'");

        if (request.UserRole != "Researcher" && request.UserRole != "Lecturer")
            throw new ArgumentException("UserRole must be 'Researcher' or 'Lecturer'");

        // Check duplicate active (exclude current)
        if (entity.Status && await _annualFeeRepo.ExistsActiveDuplicateAsync(
                request.UserRole, request.BillingCycle, id))
            throw new InvalidOperationException(
                $"An active plan already exists for {request.UserRole} × {request.BillingCycle}");

        entity.Name = request.Name;
        entity.UserRole = request.UserRole;
        entity.Price = request.Price;
        entity.BillingCycle = request.BillingCycle;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.UpdatedAt = DateTime.UtcNow;

        _annualFeeRepo.Update(entity);
        await _annualFeeRepo.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<AnnualFeeResponse?> ToggleAsync(int id, AnnualFeeToggleRequest request)
    {
        var entity = await _annualFeeRepo.GetByIdAsync(id);
        if (entity == null) return null;

        // Check duplicate if activating
        if (request.IsActive && await _annualFeeRepo.ExistsActiveDuplicateAsync(
                entity.UserRole, entity.BillingCycle, id))
            throw new InvalidOperationException(
                $"An active plan already exists for {entity.UserRole} × {entity.BillingCycle}");

        entity.Status = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _annualFeeRepo.Update(entity);
        await _annualFeeRepo.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _annualFeeRepo.GetByIdAsync(id);
        if (entity == null) return false;

        if (await _annualFeeRepo.HasTransactionsAsync(id))
            throw new InvalidOperationException(
                "Cannot delete this plan because it has historical transactions. Use toggle instead.");

        _annualFeeRepo.Delete(entity);
        await _annualFeeRepo.SaveChangesAsync();

        return true;
    }

    // ─────────────────────────────────────────────
    // PUBLIC
    // ─────────────────────────────────────────────

    public async Task<PagedResult<AnnualFeeResponse>> GetActiveAsync(int page = 1, int pageSize = 20)
    {
        return await GetAllPagedAsync(new AnnualFeeFilterParams
        {
            PageNumber = page,
            PageSize = pageSize,
            Status = true
        });
    }

    /// <summary>
    /// Public: chỉ trả plan matching role user.
    /// Researcher → plan UserRole = "Researcher"
    /// Lecturer   → plan UserRole = "Lecturer"
    /// Role khác  (Guest/Admin/Reviewer/Graduate Student) → trả rỗng
    /// </summary>
    public async Task<PagedResult<AnnualFeeResponse>> GetActiveForRoleAsync(
        string userRole, int page = 1, int pageSize = 20)
    {
        // Chỉ 2 role được phép mua gói 6/12 tháng
        if (string.IsNullOrWhiteSpace(userRole) ||
            (userRole != "Researcher" && userRole != "Lecturer"))
        {
            return new PagedResult<AnnualFeeResponse>(
                new List<AnnualFeeResponse>(), 0,
                page < 1 ? 1 : page,
                pageSize < 1 ? 20 : pageSize);
        }

        return await GetAllPagedAsync(new AnnualFeeFilterParams
        {
            PageNumber = page,
            PageSize = pageSize,
            Status = true,
            UserRole = userRole
        });
    }

    // ─────────────────────────────────────────────
    // USER SUBSCRIPTION
    // ─────────────────────────────────────────────

    public async Task<MySubscriptionResponse?> GetMySubscriptionAsync(int userId, string userRole)
    {
        var subscription = await _subscriptionRepo.GetLatestActiveAsync(userId, userRole);
        if (subscription == null) return null;

        AnnualFeeResponse? annualFee = null;
        if (subscription.LatestTransactionId.HasValue)
        {
            var tx = await _transactionRepo.GetByIdAsync(subscription.LatestTransactionId.Value);
            if (tx?.AnnualFeeId.HasValue == true)
            {
                var fee = await _annualFeeRepo.GetByIdAsync(tx.AnnualFeeId.Value);
                if (fee != null) annualFee = MapToResponse(fee);
            }
        }

        var now = DateTime.UtcNow;
        var daysRemaining = subscription.ExpiresAt.HasValue
            ? (int)Math.Max(0, (subscription.ExpiresAt.Value - now).TotalDays)
            : 0;
        var isExpired = subscription.ExpiresAt.HasValue && subscription.ExpiresAt.Value <= now;

        return new MySubscriptionResponse
        {
            Purchase = null, // TODO: embed transaction
            AnnualFee = annualFee,
            DaysRemaining = daysRemaining,
            IsExpired = isExpired
        };
    }

    public async Task<PagedResult<AnnualFeePurchaseResponse>> GetMyPurchasesAsync(
        int userId, string userRole, int page, int pageSize)
    {
        // Lấy user subscriptions
        var subscription = await _subscriptionRepo.GetByUserAndRoleAsync(userId, userRole);

        // Lấy transactions liên quan đến annual fee của user này
        var query = _transactionRepo.GetQueryable()
            .Where(t => t.AnnualFeeId != null && t.UserId == userId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var responses = new List<AnnualFeePurchaseResponse>();
        foreach (var tx in items)
        {
            var annualFee = tx.AnnualFeeId.HasValue
                ? await _annualFeeRepo.GetByIdAsync(tx.AnnualFeeId.Value)
                : null;

            responses.Add(new AnnualFeePurchaseResponse
            {
                TransactionId = tx.TransactionId,
                UserId = tx.UserId,
                AnnualFeeId = tx.AnnualFeeId,
                Amount = tx.Amount,
                Status = tx.Status,
                Description = tx.Description,
                PaymentDescription = tx.PaymentDescription,
                PaymentMethod = tx.Type,
                PaymentOrderId = tx.PaymentOrderId,
                CreatedAt = tx.CreatedAt,
                ExpiryDate = null,
                AnnualFee = annualFee == null ? null : MapToResponse(annualFee)
            });
        }

        return new PagedResult<AnnualFeePurchaseResponse>(responses, total, page, pageSize);
    }

    // ─────────────────────────────────────────────
    // PURCHASE
    // ─────────────────────────────────────────────

    public async Task<AnnualFeePurchaseResultResponse> PurchaseAsync(
        int annualFeeId, AnnualFeePurchaseRequest request, string callerRole)
    {
        // 1. Validate role
        if (callerRole != "Researcher" && callerRole != "Lecturer")
            throw new UnauthorizedAccessException("Only Researcher or Lecturer can purchase annual fee.");

        // 2. Validate plan
        var plan = await _annualFeeRepo.GetByIdAsync(annualFeeId);
        if (plan == null)
            throw new KeyNotFoundException("AnnualFee not found.");

        if (!plan.Status)
            throw new InvalidOperationException("This plan is not active and cannot be purchased.");

        // 3. UserId từ request (FE truyền xuống) hoặc lấy từ claim
        var userId = request.UserId ?? throw new ArgumentException("UserId is required.");

        // 4. Tạo orderCode
        var orderCode = $"AF-{annualFeeId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100000000}";

        // 5. Mô tả thanh toán
        var paymentDesc = $"{plan.UserRole} Payment {plan.BillingCycle}";

        // 6. Tạo transaction record
        var tx = new Transaction
        {
            Type = "ANNUAL_FEE",
            UserId = userId,
            AnnualFeeId = annualFeeId,
            Amount = plan.Price,
            Status = "PENDING_PAYMENT",
            Description = $"Annual Fee - {plan.Name}",
            PaymentDescription = paymentDesc,
            PaymentOrderId = orderCode,
            CreatedAt = DateTime.UtcNow
        };

        await _transactionRepo.AddAsync(tx);
        await _transactionRepo.SaveChangesAsync();

        // 7. Gọi PayOS tạo payment link
        var checkoutUrl = await CreatePayOSPaymentLinkAsync(
            orderCode: orderCode,
            amount: (int)plan.Price,
            description: $"ARS Platform - {plan.Name} ({paymentDesc})",
            returnUrl: request.ReturnUrl,
            cancelUrl: request.CancelUrl);

        // 8. Trả về
        return new AnnualFeePurchaseResultResponse
        {
            CheckoutUrl = checkoutUrl,
            OrderCode = orderCode,
            Purchase = new AnnualFeePurchaseResponse
            {
                TransactionId = tx.TransactionId,
                UserId = tx.UserId,
                AnnualFeeId = annualFeeId,
                Amount = plan.Price,
                Status = "PENDING_PAYMENT",
                Description = tx.Description,
                PaymentDescription = paymentDesc,
                PaymentMethod = "PayOS",
                PaymentOrderId = orderCode,
                CreatedAt = tx.CreatedAt,
                AnnualFee = MapToResponse(plan)
            }
        };
    }

    // ─────────────────────────────────────────────
    // PAYOS WEBHOOK
    // ─────────────────────────────────────────────

    public async Task<bool> ProcessPayOSWebhookAsync(AnnualFeePayOSWebhookRequest webhook)
    {
        // Tìm transaction theo orderCode
        var orderCode = webhook.OrderCode ?? webhook.OrderId?.ToString();
        if (string.IsNullOrEmpty(orderCode))
            return false;

        var tx = await _transactionRepo.GetByOrderCodeAsync(orderCode);
        if (tx == null)
            return false;

        // Idempotent: đã xử lý rồi thì bỏ qua
        if (tx.Status == "ACTIVE")
            return true;

        var code = webhook.Code ?? webhook.Status;

        if (code == "00" || code?.ToUpper() == "PAID" || code?.ToUpper() == "SUCCESS")
        {
            // ── SUCCESS ──
            tx.Status = "ACTIVE";
            tx.PaymentResponseCode = code;
            _transactionRepo.Update(tx);
            await _transactionRepo.SaveChangesAsync();

            // Tính ExpiryDate
            var plan = tx.AnnualFeeId.HasValue
                ? await _annualFeeRepo.GetByIdAsync(tx.AnnualFeeId.Value)
                : null;

            DateTime expiryDate;
            if (plan != null && plan.BillingCycle == "SixMonth")
                expiryDate = tx.CreatedAt?.AddMonths(6) ?? DateTime.UtcNow.AddMonths(6);
            else
                expiryDate = tx.CreatedAt?.AddYears(1) ?? DateTime.UtcNow.AddYears(1);

            var userRole = plan?.UserRole ?? "Researcher";

            // Cập nhật UserSubscriptions
            await UpsertUserSubscriptionAsync(tx.UserId ?? 0, userRole, expiryDate, tx.TransactionId);

            // Expire subscription cũ (single-active-policy)
            await ExpireOldSubscriptionsAsync(tx.UserId ?? 0, userRole, tx.TransactionId);
        }
        else
        {
            // ── FAILED / CANCELLED ──
            tx.Status = "FAILED";
            tx.PaymentResponseCode = code;
            _transactionRepo.Update(tx);
            await _transactionRepo.SaveChangesAsync();
        }

        return true;
    }

    // ─────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────

    private async Task UpsertUserSubscriptionAsync(
        int userId, string userRole, DateTime expiresAt, int transactionId)
    {
        var existing = await _subscriptionRepo.GetByUserAndRoleAsync(userId, userRole);

        if (existing != null)
        {
            existing.ExpiresAt = expiresAt;
            existing.LatestTransactionId = transactionId;
            existing.UpdatedAt = DateTime.UtcNow;
            _subscriptionRepo.Update(existing);
        }
        else
        {
            await _subscriptionRepo.AddAsync(new UserSubscription
            {
                UserId = userId,
                UserRole = userRole,
                ExpiresAt = expiresAt,
                LatestTransactionId = transactionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _subscriptionRepo.SaveChangesAsync();
    }

    private async Task ExpireOldSubscriptionsAsync(int userId, string userRole, int currentTransactionId)
    {
        // Mark old ACTIVE subscriptions as EXPIRED
        var oldTx = _transactionRepo.GetQueryable()
            .Where(t =>
                t.UserId == userId &&
                t.AnnualFeeId != null &&
                t.Status == "ACTIVE" &&
                t.TransactionId != currentTransactionId);

        foreach (var old in oldTx)
        {
            old.Status = "EXPIRED";
            _transactionRepo.Update(old);
        }

        await _transactionRepo.SaveChangesAsync();
    }

    private async Task<string> CreatePayOSPaymentLinkAsync(
        string orderCode, int amount, string description,
        string returnUrl, string cancelUrl)
    {
        var sigData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
        var signature = ComputeHmacSha256(sigData, _payOSSettings.ChecksumKey);

        var body = new
        {
            orderCode = orderCode,
            amount = amount,
            description = description,
            returnUrl = returnUrl,
            cancelUrl = cancelUrl,
            signature = signature
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-Client-Id", _payOSSettings.ClientId);
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _payOSSettings.ApiKey);

        var response = await _httpClient.PostAsync(
            $"{_payOSSettings.BaseUrl}/v2/payment-requests", content);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"PayOS API error: {responseContent}");

        var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        var respCode = root.TryGetProperty("code", out var c) ? c.GetString() : "";
        if (respCode != "00")
        {
            var desc = root.TryGetProperty("desc", out var d) ? d.GetString() : "Unknown";
            throw new Exception($"PayOS error {respCode}: {desc}");
        }

        var checkoutUrl = root.TryGetProperty("data", out var data) &&
                          data.TryGetProperty("checkoutUrl", out var url)
                          ? url.GetString()
                          : throw new Exception("PayOS response missing checkoutUrl");

        return checkoutUrl ?? throw new Exception("PayOS checkoutUrl is null");
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    private static AnnualFeeResponse MapToResponse(AnnualFee entity)
    {
        return new AnnualFeeResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            UserRole = entity.UserRole,
            Price = entity.Price,
            BillingCycle = entity.BillingCycle,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
