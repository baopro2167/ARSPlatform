using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IMembershipPackageRepository : IGenericRepository<MembershipPackage>
    {
        Task<Dictionary<int, int>> GetSubscriberCountsAsync();
        Task<int> GetSubscriberCountAsync(int packageId);
        Task<bool> HasPurchaseHistoryAsync(int packageId);
    }
}