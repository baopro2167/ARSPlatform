using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class MembershipPackageRepository : GenericRepository<MembershipPackage>, IMembershipPackageRepository
    {
        public MembershipPackageRepository(AppDbContext context) : base(context)
        {
        }
    }
}
