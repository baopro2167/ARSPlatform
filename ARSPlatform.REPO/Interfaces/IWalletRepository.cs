using ARSPlatform.MODEL.Entities;
using System.Threading.Tasks;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IWalletRepository : IGenericRepository<Wallet>
    {
        Task<Wallet?> GetByUserIdAsync(int userId);
    }
}