using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class DetailedEvaluationRepository : GenericRepository<DetailedEvaluation>, IDetailedEvaluationRepository
    {
        public DetailedEvaluationRepository(AppDbContext context) : base(context)
        {
        }
    }
}
