namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class GroupMemberSetLeaderRequest
    {
        public int? GroupMemberId { get; set; }
        public int? UserId { get; set; }
        public int? ResearchGroupId { get; set; }
    }
}
