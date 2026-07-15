using ARSPlatform.MODEL.Entities;
using System.Threading.Tasks;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> GetByNameAsync(string name);
    }
}
