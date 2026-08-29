using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public class ExternalApiService : IExternalApiService
    {
        public Task<bool> ValidateOrcidIdAsync(string orcidId)
        {
            var isValid = OrcidIdUtility.TryNormalizeAndValidate(
                orcidId,
                out _);

            return Task.FromResult(isValid);
        }

        public async Task<bool> ValidateDoiAsync(string doi)
        {
            await Task.Delay(50); // Mock delay

            if (string.IsNullOrWhiteSpace(doi))
                return false;

            var regex = new Regex(@"^10\.\d{4,9}/[-._;()/:A-Z0-9]+$", RegexOptions.IgnoreCase);
            return regex.IsMatch(doi);
        }

        public async Task<string?> GetPaperTitleByDoiAsync(string doi)
        {
            await Task.Delay(50); // Mock delay

            if (!await ValidateDoiAsync(doi))
                return null;

            return "CrossRef Metadata: Simulated Title for DOI " + doi;
        }
    }
}