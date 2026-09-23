using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IResearchTopicRepository : IGenericRepository<ResearchTopic>
    {
        Task<bool> AnyByLearningMaterialIdAsync(int materialId, string? fileUrl);
    }
}
