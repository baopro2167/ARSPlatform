using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class ResearchTopicLearningMaterialRepository : GenericRepository<ResearchTopicLearningMaterial>, IResearchTopicLearningMaterialRepository
    {
        public ResearchTopicLearningMaterialRepository(AppDbContext context) : base(context)
        {
        }
    }
}
