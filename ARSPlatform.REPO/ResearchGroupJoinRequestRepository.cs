using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class ResearchGroupJoinRequestRepository : GenericRepository<ResearchGroupJoinRequest>, IResearchGroupJoinRequestRepository
    {
        public ResearchGroupJoinRequestRepository(AppDbContext context) : base(context)
        {
        }
    }
}
