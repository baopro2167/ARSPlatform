namespace ARSPlatform.SERVICE
{
    /// <summary>
    /// Cấu hình cho Semantic Scholar Graph API.
    /// Semantic Scholar cung cấp API public, miễn phí, không cần API key.
    /// Tài liệu: https://api.semanticscholar.org/api-docs/
    /// </summary>
    public class SemanticScholarSettings
    {
        public string BaseUrl { get; set; } = "https://api.semanticscholar.org/graph/v1";

        public int TimeoutSeconds { get; set; } = 15;

        public int MaxPapers { get; set; } = 100;

        public int AuthorCacheSeconds { get; set; } = 3600;

        public int PaperCacheSeconds { get; set; } = 3600;
    }
}
