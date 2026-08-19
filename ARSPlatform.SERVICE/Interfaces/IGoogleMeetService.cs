using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IGoogleMeetService
    {
        Task<string> CreateMeetingSpaceAsync(
            CancellationToken cancellationToken = default);
    }
}