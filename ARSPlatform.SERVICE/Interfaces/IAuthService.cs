using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterRequest request);
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<AuthResponse?> GoogleLoginAsync(GoogleLoginRequest request);
        Task<bool> VerifyEmailAsync(string token);
        Task<bool> SendApprovalEmailAsync(string email);
        string GetGoogleAuthorizationUrl(string redirectUri);
        Task<string?> ExchangeCodeForRefreshTokenAsync(string code, string redirectUri);
    }
}
