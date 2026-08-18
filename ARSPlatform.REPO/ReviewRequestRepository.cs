using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class ReviewRequestRepository : GenericRepository<ReviewRequest>, IReviewRequestRepository
    {
        public ReviewRequestRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ReviewRequest>> GetAllWithReviewerAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Reviewer)
                .Include(x => x.Paper)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.ReviewRequestId)
                .ToListAsync();
        }

        public async Task<ReviewRequest?> GetByIdWithReviewerAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Reviewer)
                .Include(x => x.Paper)
                .FirstOrDefaultAsync(x => x.ReviewRequestId == id);
        }
    }
}