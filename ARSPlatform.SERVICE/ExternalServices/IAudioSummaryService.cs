using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public interface IAudioSummaryService
    {
        Task<SeminarAudioSummaryResponse> SummarizeSeminarAudioAsync(int seminarId, SeminarAudioSummaryRequest request, CancellationToken cancellationToken = default);
    }
}