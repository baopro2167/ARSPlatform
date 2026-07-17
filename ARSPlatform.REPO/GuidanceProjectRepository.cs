using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class GuidanceProjectRepository : GenericRepository<GuidanceProject>, IGuidanceProjectRepository
    {
        public GuidanceProjectRepository(AppDbContext context) : base(context)
        {
        }
    }
}
