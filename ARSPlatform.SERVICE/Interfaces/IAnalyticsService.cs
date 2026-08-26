using System.Threading;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IAnalyticsService
    {
        Task<AnalyticsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
        Task<AnalyticsTimeseriesResponse> GetTimeseriesAsync(string range, string metric, CancellationToken cancellationToken);
    }
}
