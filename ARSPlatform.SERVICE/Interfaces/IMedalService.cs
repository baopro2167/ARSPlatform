using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IMedalService
    {
        // Admin Endpoints
        Task<IEnumerable<MedalResponse>> GetAllAsync(string? role = null, string? tier = null, bool? isActive = null, string? search = null);
        Task<MedalResponse?> GetByIdAsync(string id);
        Task<MedalResponse> CreateAsync(MedalCreateRequest request);
        Task<MedalResponse?> UpdateAsync(string id, MedalUpdateRequest request);
        Task<bool> DeleteAsync(string id);
        Task<IEnumerable<MedalResponse>> ResetToDefaultsAsync();

        // User Endpoints
        Task<IEnumerable<UserMedalResponse>> GetMyMedalsAsync(int userId);
        Task<IEnumerable<UserMedalResponse>> GetUserUnlockedMedalsAsync(int userId);
        Task<IEnumerable<UserMedalResponse>> GetUserMedalsAsync(int userId, bool includeLocked, int? callerId, bool isAdmin);
        Task EvaluateUserMedalsAsync(int userId);

        // Admin Manual Grant & Dev Helpers (Ticket BE-MEDAL-GRANT-01)
        Task<(UserMedalResponse Response, bool IsCreated)> GrantMedalAsync(MedalGrantRequest request, int adminId, string adminName);
        Task<bool> RevokeGrantedMedalAsync(long userMedalId, int adminId, string adminName);
        Task<MedalDevGrantAllResponse> DevGrantAllByRoleAsync(MedalDevGrantAllRequest request, int adminId, string adminName);
        Task<MedalDevRevokeAllResponse> DevRevokeAllAsync(int userId, int adminId, string adminName);
    }
}
