using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class OrcidLookupRequest
    {
        [Range(1, int.MaxValue)]
        public int RoleRequestId { get; set; }
    }
}