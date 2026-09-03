namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarStatsResponse
    {
        public int SeminarId { get; set; }
        public int TotalInvited { get; set; }
        public int Submitted { get; set; }
        public int Pending { get; set; }
        public int Declined { get; set; }
        public decimal CompletionPercentage { get; set; }
    }
}