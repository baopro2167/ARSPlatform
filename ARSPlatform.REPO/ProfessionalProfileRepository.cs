using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class ProfessionalProfileRepository : GenericRepository<ProfessionalProfile>, IProfessionalProfileRepository
    {
        public ProfessionalProfileRepository(AppDbContext context) : base(context)
        {
        }
    }
}
