using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class FollowerRepository : GenericRepository<Follower>, IFollowerRepository
    {
        public FollowerRepository(AppDbContext context) : base(context)
        {
        }
    }
}
