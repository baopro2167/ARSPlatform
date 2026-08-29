using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class OrcidStatusResponse
    {
        public int UserId { get; set; }

        public bool IsConnected { get; set; }

        public bool IsVerified { get; set; }

        public string? OrcidId { get; set; }

        public string? DisplayName { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public bool CanConnect { get; set; }
    }
}