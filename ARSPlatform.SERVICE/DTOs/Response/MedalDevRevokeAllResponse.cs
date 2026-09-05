namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MedalDevRevokeAllResponse
    {
        public int UserId { get; set; }
        public int RevokedCount { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }
}
