using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    /// <summary>
    /// Request DTO cho các endpoint Semantic Scholar.
    /// Truyền <see cref="AuthorId"/> để lấy profile, hoặc <see cref="Query"/> để search theo tên.
    /// </summary>
    public class ScholarSearchRequest
    {
        /// <summary>
        /// Tên tác giả cần tìm (chỉ dùng cho endpoint /author/search).
        /// </summary>
        [MaxLength(200)]
        public string? Query { get; set; }

        /// <summary>
        /// Semantic Scholar Author ID (vd: "1741101"). Dùng cho /author/{id} và /author/{id}/papers.
        /// </summary>
        [MaxLength(100)]
        public string? AuthorId { get; set; }

        /// <summary>
        /// Số lượng kết quả tối đa (1-1000).
        /// </summary>
        [Range(1, 1000)]
        public int Limit { get; set; } = 10;
    }
}
