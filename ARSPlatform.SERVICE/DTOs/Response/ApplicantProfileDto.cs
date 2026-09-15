namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ApplicantProfileDto
    {
        public int UserId { get; set; }

        public string DisplayName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public string? Major { get; set; }

        public string? AcademicLevel { get; set; }
    }
}
