using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ISeminarParticipantRepository
    {
        Task<IEnumerable<SeminarParticipant>> GetAllAsync();

        Task<SeminarParticipant?> GetByIdAsync(int seminarParticipantId);

        Task<IEnumerable<SeminarParticipant>> GetBySeminarIdAsync(int seminarId);

        Task<SeminarParticipant> CreateAsync(
            SeminarParticipant seminarParticipant);

        Task<SeminarParticipant> UpdateAsync(
            SeminarParticipant seminarParticipant);

        Task<bool> DeleteAsync(int seminarParticipantId);
    }
}