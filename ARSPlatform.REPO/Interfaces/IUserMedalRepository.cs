using ARSPlatform.MODEL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IUserMedalRepository : IGenericRepository<UserMedal>
    {
        Task<IEnumerable<UserMedal>> GetByUserIdWithMedalsAsync(int userId);
        Task<IEnumerable<UserMedal>> GetUnlockedByUserIdAsync(int userId);
        Task<UserMedal?> GetByUserAndMedalIdAsync(int userId, string medalId);
        Task<List<UserMedal>> GetAllByUserIdAsync(int userId);
        Task<int> CountUnlockedAsync();
        Task<int> CountUnlockedByMedalIdAsync(string medalId);
        Task<List<UserMedal>> GetLeaderboardAsync(string medalId, int topN);
    }
}
