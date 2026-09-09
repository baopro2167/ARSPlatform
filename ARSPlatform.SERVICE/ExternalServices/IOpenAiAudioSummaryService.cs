using ARSPlatform.MODEL.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public interface IOpenAiAudioSummaryService
    {
        Task<string> SummarizeAsync(
            string compressedAudioPath, Seminar seminar, CancellationToken cancellationToken = default);
    }
}