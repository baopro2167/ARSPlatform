using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class PhasedReportRepository : GenericRepository<PhasedReport>, IPhasedReportRepository
    {
        public PhasedReportRepository(AppDbContext context) : base(context)
        {
        }
    }
}
