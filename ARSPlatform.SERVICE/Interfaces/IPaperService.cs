using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IPaperService
    {
        Task<PagedResult<PaperResponse>> GetPapersAsync(
            PaginationParams paginationParams);

        Task<PagedResult<PaperResponse>> GetByAuthorIdAsync(
            int authorId,
            int pageNumber,
            int pageSize);

        Task<PagedResult<PaperResponse>> GetBySubFieldIdAsync(
            int subFieldId,
            int pageNumber,
            int pageSize);

        Task<PagedResult<PaperResponse>> GetAllAsync(
            int pageNumber,
            int pageSize);

        Task<PaperResponse?> GetPaperByIdAsync(int id);

        Task<PaperResponse> CreatePaperAsync(
            PaperCreateRequest request,
            int authorId);

        Task<PaperResponse?> UpdatePaperAsync(
            int id,
            PaperUpdateRequest request,
            bool allowStatusUpdate = false);

        Task<bool> DeletePaperAsync(int id);

        Task<PaperAuthorshipVerificationResponse?>
            VerifyAuthorshipAsync(
                int paperId,
                PaperAuthorshipVerifyRequest request);

        /// <summary>
        /// Lấy danh sách paper được phân công cho 1 reviewer.
        /// Response gồm paper kèm <c>ReviewerId</c> và <c>ReviewerName</c> (User.FullName).
        /// </summary>
        Task<List<PaperWithReviewerResponse>> GetPapersByReviewerAsync(
            int reviewerId);
    }
}