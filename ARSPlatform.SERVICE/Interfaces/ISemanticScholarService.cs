using ARSPlatform.SERVICE.DTOs.Response;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.Interfaces
{
    /// <summary>
    /// Service tích hợp Semantic Scholar Graph API (free public API).
    /// Tài liệu: https://api.semanticscholar.org/api-docs/
    /// </summary>
    public interface ISemanticScholarService
    {
        /// <summary>
        /// Lấy profile + chỉ số trích dẫn của một tác giả theo Author ID.
        /// </summary>
        Task<ScholarAuthorProfileResponse> GetAuthorAsync(
            string authorId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm kiếm tác giả theo tên (tối đa <paramref name="limit"/> kết quả, 1-1000).
        /// </summary>
        Task<ScholarSearchResponse> SearchAuthorsAsync(
            string query,
            int limit,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách bài báo của một tác giả (tối đa <paramref name="limit"/> bài, 1-1000).
        /// </summary>
        Task<ScholarAuthorPapersResponse> GetAuthorPapersAsync(
            string authorId,
            int limit,
            CancellationToken cancellationToken = default);
    }
}
