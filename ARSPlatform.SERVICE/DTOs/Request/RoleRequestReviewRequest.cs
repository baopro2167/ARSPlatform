namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class RoleRequestReviewRequest
    {
        public string Status { get; set; } = "ACCEPTED";
        public string? AdminNotes { get; set; }
        public string? Notes { get; set; }
    }
}
