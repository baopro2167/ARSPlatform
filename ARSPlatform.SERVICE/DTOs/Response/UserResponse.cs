using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? GoogleId { get; set; }
        public string? AvatarUrl { get; set; }
        public bool? IsEmailVerified { get; set; }
        public bool? IsActive { get; set; }
        public int? AccountTier { get; set; }
        public string? VerificationStatus { get; set; }
        public string? ProofDocumentUrl { get; set; }

        public string? OrcidId { get; set; }
        public string? OrcidDisplayName { get; set; }
        public bool IsOrcidVerified { get; set; }
        public DateTime? OrcidVerifiedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
