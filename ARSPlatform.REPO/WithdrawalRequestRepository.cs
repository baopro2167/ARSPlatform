using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class WithdrawalRequestRepository
        : GenericRepository<WithdrawalRequest>,
          IWithdrawalRequestRepository
    {
        public WithdrawalRequestRepository(AppDbContext context)
            : base(context)
        {
        }
    }
}