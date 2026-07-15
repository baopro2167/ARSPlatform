using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public interface IExternalApiService
    {
        Task<bool> ValidateOrcidIdAsync(string orcidId);
        Task<bool> ValidateDoiAsync(string doi);
        Task<string?> GetPaperTitleByDoiAsync(string doi);
    }
}
