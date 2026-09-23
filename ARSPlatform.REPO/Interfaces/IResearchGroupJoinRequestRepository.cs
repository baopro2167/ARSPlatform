using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IResearchGroupJoinRequestRepository : IGenericRepository<ResearchGroupJoinRequest>
    {
        Task<ResearchGroupJoinRequest?> GetWithDetailsAsync(int joinRequestId, int? groupId = null);
        Task<bool> HasPendingRequestAsync(int userId, int groupId);
        Task<IEnumerable<ResearchGroupJoinRequest>> GetListWithDetailsAsync(int groupId, string? status);
    }
}
