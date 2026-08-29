namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class RoleRequestResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Affiliation { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public List<string> CurrentRoles { get; set; } = new();
        public List<string> RequestedAdditionalRoles { get; set; } = new();
        public string? RequestType { get; set; }
        public List<string> RequestedRoles { get; set; } = new();

        public string? OrcidId { get; set; }
        public bool IsOrcidVerified { get; set; }
        public DateTime? OrcidVerifiedAt { get; set; }

        public string ProofDocumentUrl { get; set; } = string.Empty;
        public bool? IsEmailVerified { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}