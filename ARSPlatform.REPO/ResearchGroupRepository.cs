using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class ResearchGroupRepository : GenericRepository<ResearchGroup>, IResearchGroupRepository
    {
        public ResearchGroupRepository(AppDbContext context) : base(context)
        {
        }
    }
}
