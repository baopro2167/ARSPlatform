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
    }
}