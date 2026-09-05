namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class UserUpdateRequest
    {
        public string? FullName { get; set; }

        public string? AvatarUrl { get; set; }

        public bool? IsActive { get; set; }
    }
}
