using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Transaction?> GetByOrderCodeAsync(string orderCode)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaymentOrderId == orderCode);
        }
    }
}
