using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class MajorFieldRepository : GenericRepository<MajorField>, IMajorFieldRepository
    {
        public MajorFieldRepository(AppDbContext context) : base(context)
        {
        }
    }
}
