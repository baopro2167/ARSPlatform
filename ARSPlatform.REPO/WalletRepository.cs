using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class WalletRepository : GenericRepository<Wallet>, IWalletRepository
    {
        public WalletRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Wallet?> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}