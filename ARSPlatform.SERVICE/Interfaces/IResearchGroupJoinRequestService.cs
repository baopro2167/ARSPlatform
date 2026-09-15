using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IResearchGroupJoinRequestService
    {
        Task<ResearchGroupJoinRequestResponse> CreateJoinRequestAsync(int groupId, int applicantUserId, string? note = null);

        Task<IEnumerable<ResearchGroupJoinRequestResponse>> GetJoinRequestsForLecturerAsync(int groupId, int currentUserId, string? status = null);

        Task<ResearchGroupJoinRequestResponse?> GetJoinRequestByIdAsync(int groupId, int joinRequestId, int currentUserId);

        Task<ResearchGroupJoinRequestResponse> AcceptJoinRequestAsync(int groupId, int joinRequestId, int currentUserId);

        Task<ResearchGroupJoinRequestResponse> RejectJoinRequestAsync(int groupId, int joinRequestId, string? rejectionNote, int currentUserId);
    }
}
