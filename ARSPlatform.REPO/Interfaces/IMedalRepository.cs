using ARSPlatform.MODEL.Entities;
using System.Threading.Tasks;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IMedalRepository : IGenericRepository<Medal>
    {
        Task<IEnumerable<Medal>> GetAllWithFiltersAsync(string? role, string? tier, bool? isActive, string? search);
        Task<Medal?> GetByCodeAsync(string code);
        Task<bool> ExistsByCodeAsync(string code, string? excludeId = null);
        Task<List<Medal>> GetActiveAsync();
        Task<int> CountActiveAsync();
    }
}
