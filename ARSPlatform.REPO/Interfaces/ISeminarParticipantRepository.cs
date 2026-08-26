using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ISeminarParticipantRepository : IGenericRepository<SeminarParticipant>
    {
        Task<IEnumerable<SeminarParticipant>> GetAllWithUserAsync();

        Task<IEnumerable<SeminarParticipant>>
            GetAllForOrganizerWithUserAsync(int organizerId);

        Task<IEnumerable<SeminarParticipant>>
            GetBySeminarIdWithUserAsync(int seminarId);

        Task<PagedResult<SeminarParticipant>> GetBySeminarIdPagedAsync(int seminarId, PaginationParams paginationParams);
        Task<PagedResult<SeminarParticipant>> GetBySeminarIdPagedAsync(int seminarId, int pageNumber, int pageSize);
        Task<PagedResult<SeminarParticipant>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams);
        Task<PagedResult<SeminarParticipant>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize);

        Task<SeminarParticipant?>
            GetByIdWithSeminarAndUserAsync(int id);
    }
}