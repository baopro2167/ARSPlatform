namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class OrcidLookupRequest
    {
        public string OrcidId { get; set; } = string.Empty;

        public int? RoleRequestId { get; set; }
    }
}