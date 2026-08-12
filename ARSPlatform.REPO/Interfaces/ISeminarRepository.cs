using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ISeminarRepository
    {
        Task<IEnumerable<Seminar>> GetAllAsync();

        Task<Seminar?> GetByIdAsync(int seminarId);

        Task<Seminar> CreateAsync(Seminar seminar);

        Task<Seminar> UpdateAsync(Seminar seminar);

        Task<bool> DeleteAsync(int seminarId);
    }
}