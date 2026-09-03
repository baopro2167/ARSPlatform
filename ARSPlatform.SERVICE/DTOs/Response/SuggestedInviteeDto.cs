using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SuggestedInviteeDto
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string? Role { get; set; }

        public int? SubFieldId { get; set; }

        public string? SubFieldName { get; set; }

        public string? OrcidId { get; set; }

        public int? Hindex { get; set; }

        public int? PublicationCount { get; set; }
    }
}
