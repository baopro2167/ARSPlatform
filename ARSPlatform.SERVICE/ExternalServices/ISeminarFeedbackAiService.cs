using ARSPlatform.SERVICE.DTOs.Response;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.ExternalServices
{
    public interface ISeminarFeedbackAiService
    {
        Task<SeminarFeedbackAiSummaryContentResponse> SummarizeFeedbackAsync(string seminarContent, IReadOnlyCollection<string> feedbackJsons, CancellationToken cancellationToken = default);
    }
}