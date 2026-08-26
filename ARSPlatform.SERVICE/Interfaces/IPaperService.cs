using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using System;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IPaperService
    {
        Task<PagedResult<PaperResponse>> GetPapersAsync(PaginationParams paginationParams);
        Task<PagedResult<PaperResponse>> GetByAuthorIdAsync(int authorId, int pageNumber, int pageSize);
        Task<PagedResult<PaperResponse>> GetBySubFieldIdAsync(int subFieldId, int pageNumber, int pageSize);
        Task<PagedResult<PaperResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<PaperResponse?> GetPaperByIdAsync(int id);
        Task<PaperResponse> CreatePaperAsync(PaperCreateRequest request, int authorId);
        Task<PaperResponse?> UpdatePaperAsync(int id, PaperUpdateRequest request);
        Task<bool> DeletePaperAsync(int id);
    }
}
