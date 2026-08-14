using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class SubFieldRepository : GenericRepository<SubField>, ISubFieldRepository
    {
        public SubFieldRepository(AppDbContext context) : base(context)
        {
        }
    }
}
