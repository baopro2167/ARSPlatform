using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ISeminarParticipantRepository : IGenericRepository<SeminarParticipant>
    {
        Task<IEnumerable<SeminarParticipant>> GetAllWithUserAsync();

        Task<IEnumerable<SeminarParticipant>>
            GetAllForOrganizerWithUserAsync(int organizerId);

        Task<IEnumerable<SeminarParticipant>>
            GetBySeminarIdWithUserAsync(int seminarId);

        Task<SeminarParticipant?>
            GetByIdWithSeminarAndUserAsync(int id);
    }
}