namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class OrcidLinkCallbackResponse
    {
        public bool Success { get; set; }

        public string? Context { get; set; }

        public string? Status { get; set; }

        public string? OrcidId { get; set; }

        public string? DisplayName { get; set; }

        public string? RegistrationTicket { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }
    }
}