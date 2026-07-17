using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class SharedMaterialRepository : GenericRepository<SharedMaterial>, ISharedMaterialRepository
    {
        public SharedMaterialRepository(AppDbContext context) : base(context)
        {
        }
    }
}
