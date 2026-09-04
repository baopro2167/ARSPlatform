using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IUserMedalRepository : IGenericRepository<UserMedal>
    {
        Task<IEnumerable<UserMedal>> GetByUserIdWithMedalsAsync(int userId);
        Task<IEnumerable<UserMedal>> GetUnlockedByUserIdAsync(int userId);
        Task<UserMedal?> GetByUserAndMedalIdAsync(int userId, string medalId);
    }
}
