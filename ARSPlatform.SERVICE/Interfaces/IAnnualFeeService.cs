using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces;

public interface IAnnualFeeService
{
    // Admin CRUD
    Task<PagedResult<AnnualFeeResponse>> GetAllPagedAsync(AnnualFeeFilterParams filter);
    Task<AnnualFeeResponse?> GetByIdAsync(int id);
    Task<AnnualFeeResponse> CreateAsync(AnnualFeeCreateRequest request);
    Task<AnnualFeeResponse?> UpdateAsync(int id, AnnualFeeUpdateRequest request);
    Task<AnnualFeeResponse?> ToggleAsync(int id, AnnualFeeToggleRequest request);
    Task<bool> DeleteAsync(int id);

    // Public
    Task<PagedResult<AnnualFeeResponse>> GetActiveAsync(int page = 1, int pageSize = 20);
    Task<AnnualFeeResponse?> GetActiveByIdAsync(int id);

    // Public: chỉ trả plan theo role user (Researcher | Lecturer); role khác → trả rỗng
    Task<PagedResult<AnnualFeeResponse>> GetActiveForRoleAsync(string userRole, int page = 1, int pageSize = 20);

    // User subscription
    Task<MySubscriptionResponse?> GetMySubscriptionAsync(int userId, string userRole);
    Task<PagedResult<AnnualFeePurchaseResponse>> GetMyPurchasesAsync(int userId, string userRole, int page, int pageSize);

    // Purchase
    Task<AnnualFeePurchaseResultResponse> PurchaseAsync(int annualFeeId, AnnualFeePurchaseRequest request, string callerRole);

    // Webhook
    Task<bool> ProcessPayOSWebhookAsync(AnnualFeePayOSWebhookRequest webhook);

    // Admin: danh sách user đang sử dụng 1 gói AnnualFee (phân trang)
    Task<PagedResult<AnnualFeeSubscriberResponse>> GetSubscribersByAnnualFeeIdAsync(int annualFeeId, PaginationParams paginationParams);
}
