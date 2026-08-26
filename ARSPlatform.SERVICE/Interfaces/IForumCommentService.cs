using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IForumCommentService
    {
        Task<IEnumerable<ForumCommentResponse>> GetAllAsync();
        Task<ForumCommentResponse?> GetByIdAsync(int id);
        Task<ForumCommentResponse> CreateAsync(ForumCommentCreateRequest request, int userId);
        Task<ForumCommentResponse?> UpdateAsync(int id, ForumCommentUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
