using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class CreateAdditionalRoleRequest
    {
        public int? UserId { get; set; }

        [Required]
        public string RequestedRole { get; set; } = string.Empty;

        public string RequestType { get; set; } = "ADDITIONAL_ROLE";

        public string? Affiliation { get; set; }

        public string? Department { get; set; }

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Proof document (PDF) is required for role verification.")]
        public string ProofDocumentUrl { get; set; } = string.Empty;

        public string? OrcidId { get; set; }

        public string? Reason { get; set; }
    }
}
