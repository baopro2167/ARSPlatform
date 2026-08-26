using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ICommentVoteService
    {
        Task<IEnumerable<CommentVoteResponse>> GetAllAsync();
        Task<CommentVoteResponse> CreateAsync(CommentVoteCreateRequest request);
    }
}
