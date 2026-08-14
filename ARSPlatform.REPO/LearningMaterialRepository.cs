using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class LearningMaterialRepository : GenericRepository<LearningMaterial>, ILearningMaterialRepository
    {
        public LearningMaterialRepository(AppDbContext context) : base(context)
        {
        }
    }
}
