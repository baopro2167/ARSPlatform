using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IProfileRepository : IGenericRepository<Profile>
    {
        Task<IEnumerable<Profile>> GetAllWithUserAsync();
        Task<Profile?> GetByIdWithUserAsync(int userId);
    }
}