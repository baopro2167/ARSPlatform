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

        public async System.Threading.Tasks.Task<bool> AnyByLearningMaterialIdAsync(int materialId, string? fileUrl)
        {
            var idString = materialId.ToString();
            var hasFileUrl = !string.IsNullOrWhiteSpace(fileUrl);

            bool isInMaterials = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(_context.ResearchTopicLearningMaterials, x => x.LearningMaterialId == materialId);
            if (isInMaterials) return true;

            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(_dbSet, t =>
                t.GuidanceProjectsUrl != null &&
                (
                    (hasFileUrl && t.GuidanceProjectsUrl.Contains(fileUrl!)) ||
                    t.GuidanceProjectsUrl == idString ||
                    t.GuidanceProjectsUrl.Contains($"/materials/{materialId}") ||
                    t.GuidanceProjectsUrl.Contains($"/learning-materials/{materialId}") ||
                    t.GuidanceProjectsUrl.Contains($"materialId={materialId}")
                ));
        }
    }
}
