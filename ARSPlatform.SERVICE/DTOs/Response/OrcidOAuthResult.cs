namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class OrcidOAuthResult
    {
        public bool Success { get; set; }

        public string? OrcidId { get; set; }

        public string? DisplayName { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }
    }
}