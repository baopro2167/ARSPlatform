using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IOrcidLinkService
    {
        Task<OrcidLinkStartResponse> StartRegistrationAsync(
            CancellationToken cancellationToken = default);

        Task<OrcidLinkStartResponse> StartAccountLinkAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<OrcidLinkCallbackResponse> HandleCallbackAsync(
            string? code,
            string? state,
            string? error,
            CancellationToken cancellationToken = default);

        Task<OrcidStatusResponse?> GetStatusAsync(
            int userId,
            CancellationToken cancellationToken = default);
    }
}