using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class RoleRequestRepository : GenericRepository<RoleRequest>, IRoleRequestRepository
    {
        public RoleRequestRepository(AppDbContext context) : base(context)
        {
        }
    }
}