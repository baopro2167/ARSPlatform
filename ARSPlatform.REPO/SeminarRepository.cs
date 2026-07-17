using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class SeminarRepository : GenericRepository<Seminar>, ISeminarRepository
    {
        public SeminarRepository(AppDbContext context) : base(context)
        {
        }
    }
}
