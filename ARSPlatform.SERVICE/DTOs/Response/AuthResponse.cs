namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Role { get; set; }
        public bool? IsEmailVerified { get; set; }
        public bool? IsActive { get; set; }
        public string? VerificationStatus { get; set; }
        public bool? IsNewUser { get; set; }
        public System.Collections.Generic.List<string>? Roles { get; set; }
        public bool? RequiresOnboarding { get; set; }
        public string? EffectiveRole { get; set; }
    }
}