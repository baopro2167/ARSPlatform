using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class ResearchTopicRepository : GenericRepository<ResearchTopic>, IResearchTopicRepository
    {
        public ResearchTopicRepository(AppDbContext context) : base(context)
        {
        }

        public async System.Threading.Tasks.Task<bool> AnyByLearningMaterialIdAsync(int materialId, string? fileUrl)
        {
            return await _context.ResearchTopicLearningMaterials.AnyAsync(x => x.LearningMaterialId == materialId);
        }
    }
}
