using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IMajorFieldRepository : IGenericRepository<MajorField>
    {
        Task<IEnumerable<MajorField>> GetAllWithSubFieldsAsync();
        Task<MajorField?> GetByIdWithSubFieldsAsync(int id);
        Task<bool> HasSubFieldsAsync(int id);
    }
}