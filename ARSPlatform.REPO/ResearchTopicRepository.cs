using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class ResearchTopicRepository : GenericRepository<ResearchTopic>, IResearchTopicRepository
    {
        public ResearchTopicRepository(AppDbContext context) : base(context)
        {
        }
    }
}
