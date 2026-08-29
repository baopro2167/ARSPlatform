using ARSPlatform.SERVICE.DTOs.Response;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IOpenAlexService
    {
        Task<OrcidLookupResponse> LookupByOrcidAsync(
            string orcidId,
            CancellationToken cancellationToken = default);

        Task<OpenAlexWorkLookupResponse> LookupWorkByIdAsync(
            string openAlexWorkId,
            CancellationToken cancellationToken = default);

        Task<OpenAlexWorkPreviewResponse> GetWorkPreviewByIdAsync(
            string workId,
            CancellationToken cancellationToken = default);
    }
}