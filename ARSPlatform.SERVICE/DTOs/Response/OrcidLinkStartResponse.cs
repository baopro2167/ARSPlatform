namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class OrcidLinkStartResponse
    {
        public string AuthorizationUrl { get; set; }
            = string.Empty;

        public string Context { get; set; }
            = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}