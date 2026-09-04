using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IMedalRepository : IGenericRepository<Medal>
    {
        Task<IEnumerable<Medal>> GetAllWithFiltersAsync(string? role, string? tier, bool? isActive, string? search);
        Task<Medal?> GetByCodeAsync(string code);
        Task<bool> ExistsByCodeAsync(string code, string? excludeId = null);
    }
}
