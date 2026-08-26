using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IFollowerService
    {
        Task<IEnumerable<FollowerResponse>> GetAllAsync();
        Task<FollowerResponse> CreateAsync(FollowerCreateRequest request);
    }
}
