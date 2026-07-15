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
        Task<PaperResponse?> GetPaperByIdAsync(Guid id);
        Task<PaperResponse> CreatePaperAsync(PaperCreateRequest request, Guid authorId);
        Task<PaperResponse?> UpdatePaperAsync(Guid id, PaperUpdateRequest request);
        Task<bool> DeletePaperAsync(Guid id);
    }
}
