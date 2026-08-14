using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class MembershipPurchaseRepository : GenericRepository<MembershipPurchase>, IMembershipPurchaseRepository
    {
        public MembershipPurchaseRepository(AppDbContext context) : base(context)
        {
        }
    }
}
