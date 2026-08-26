using System.Threading;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IAdminService
    {
        Task<(OrcidLookupResponse Response, int StatusCode)> LookupOrcidAsync(
            OrcidLookupRequest request,
            int adminId,
            string adminName,
            string correlationId,
            CancellationToken cancellationToken);
    }
}
