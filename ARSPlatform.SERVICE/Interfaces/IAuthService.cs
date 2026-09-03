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
        string GetGoogleAuthorizationUrl(string redirectUri, string scopes);
        Task<string?> ExchangeCodeForRefreshTokenAsync(string code, string redirectUri);
        Task<AuthResponse?> AuthenticateGoogleLoginAsync(string code, string redirectUri);
        Task<AuthResponse?> CompleteGoogleRegistrationAsync(CompleteGoogleRegistrationRequest request);
        Task<AuthResponse?> SelectRoleAsync(int userId, string roleName);
        Task<bool> VerifyOtpAsync(string email, string otpCode);
        Task<string?> ResendOtpAsync(string email);
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        Task<bool> UpdateExpiresAtAsync(int userId, DateTime expiresAt);
    }
}
