using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
namespace ARSPlatform.REPOSITORIES
{
    public class SeminarParticipantRepository : GenericRepository<SeminarParticipant>, ISeminarParticipantRepository
    {
        public SeminarParticipantRepository(AppDbContext context) : base(context)
        {
        }
    }
}
