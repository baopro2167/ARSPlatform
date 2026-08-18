using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ISubFieldRepository : IGenericRepository<SubField>
    {
        Task<IEnumerable<SubField>> GetAllWithMajorFieldAsync(int? majorFieldId = null);
        Task<SubField?> GetByIdWithMajorFieldAsync(int id);
        Task<bool> HasUsageAsync(int id);
    }
}