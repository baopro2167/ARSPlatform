using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class ReviewRequestRepository : GenericRepository<ReviewRequest>, IReviewRequestRepository
    {
        public ReviewRequestRepository(AppDbContext context) : base(context)
        {
        }
    }
}
