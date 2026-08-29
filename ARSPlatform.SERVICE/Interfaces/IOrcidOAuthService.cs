using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IOrcidOAuthService
    {
        string BuildAuthorizationUrl(string state);

        Task<OrcidOAuthResult> ExchangeCodeAsync(
            string code,
            CancellationToken cancellationToken = default);
    }
}