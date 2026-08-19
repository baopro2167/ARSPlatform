using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IProfessionalProfileRepository : IGenericRepository<ProfessionalProfile>
    {
        Task<IEnumerable<ProfessionalProfile>> GetAllWithUserAndFieldAsync();
        Task<ProfessionalProfile?> GetByIdWithUserAndFieldAsync(int userId);
        Task<ProfessionalProfile?> UpdateAvailabilityAsync(int userId, bool isAvailable);
    }
}