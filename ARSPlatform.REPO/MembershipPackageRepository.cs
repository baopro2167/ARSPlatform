using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class MembershipPackageRepository : GenericRepository<MembershipPackage>, IMembershipPackageRepository
    {
        public MembershipPackageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Dictionary<int, int>> GetSubscriberCountsAsync()
        {
            return await _context.MembershipPurchases
                .AsNoTracking()
                .Where(x => x.PackageId.HasValue)
                .GroupBy(x => x.PackageId!.Value)
                .Select(group => new
                {
                    PackageId = group.Key,
                    SubscriberCount = group.Count()
                })
                .ToDictionaryAsync(
                    x => x.PackageId,
                    x => x.SubscriberCount);
        }

        public async Task<int> GetSubscriberCountAsync(int packageId)
        {
            return await _context.MembershipPurchases
                .AsNoTracking()
                .CountAsync(x => x.PackageId == packageId);
        }

        public async Task<bool> HasPurchaseHistoryAsync(int packageId)
        {
            return await _context.MembershipPurchases
                .AsNoTracking()
                .AnyAsync(x => x.PackageId == packageId);
        }
    }
}