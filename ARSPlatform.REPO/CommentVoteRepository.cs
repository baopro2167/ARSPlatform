using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class CommentVoteRepository : GenericRepository<CommentVote>, ICommentVoteRepository
    {
        public CommentVoteRepository(AppDbContext context) : base(context)
        {
        }
    }
}
