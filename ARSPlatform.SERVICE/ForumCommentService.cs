using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class ForumCommentService : IForumCommentService
    {
        private readonly IForumCommentRepository _repository;
        private readonly ICommentVoteRepository _voteRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public ForumCommentService(
            IForumCommentRepository repository,
            ICommentVoteRepository voteRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IMapper mapper)
        {
            _repository = repository;
            _voteRepository = voteRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ForumCommentResponse>> GetAllAsync(int? postId = null, int? currentUserId = null)
        {
            Expression<Func<ForumComment, bool>>? predicate = postId.HasValue ? x => x.ForumPostId == postId.Value : null;
            var items = await _repository.GetAllAsync(predicate, includes: x => x.User!);
            var dtos = _mapper.Map<List<ForumCommentResponse>>(items);

            if (currentUserId.HasValue && dtos.Any())
            {
                var commentIds = dtos.Select(d => d.ForumCommentId).ToList();
                var votedIds = await _voteRepository.GetVotedCommentIdsByUserAsync(currentUserId.Value, commentIds);
                var votedSet = new HashSet<int>(votedIds);
                foreach (var dto in dtos)
                {
                    dto.IsUpvoted = votedSet.Contains(dto.ForumCommentId);
                }
            }

            return dtos;
        }

        public async Task<PagedResult<ForumCommentResponse>> GetPagedAsync(PaginationParams paginationParams, int? postId = null, int? currentUserId = null)
        {
            Expression<Func<ForumComment, bool>>? predicate = postId.HasValue ? x => x.ForumPostId == postId.Value : null;
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate,
                orderBy: q => q.OrderBy(x => x.CreatedAt),
                includes: new Expression<Func<ForumComment, object>>[]
                {
                    x => x.User!
                });
            var dtos = _mapper.Map<List<ForumCommentResponse>>(paged.Items);

            if (currentUserId.HasValue && dtos.Any())
            {
                var commentIds = dtos.Select(d => d.ForumCommentId).ToList();
                var votedIds = await _voteRepository.GetVotedCommentIdsByUserAsync(currentUserId.Value, commentIds);
                var votedSet = new HashSet<int>(votedIds);
                foreach (var dto in dtos)
                {
                    dto.IsUpvoted = votedSet.Contains(dto.ForumCommentId);
                }
            }

            return new PagedResult<ForumCommentResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ForumCommentResponse>> GetByPostIdAsync(int postId, int pageNumber, int pageSize, int? currentUserId = null)
        {
            var paged = await _repository.GetByPostIdPagedAsync(postId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ForumCommentResponse>>(paged.Items);

            if (currentUserId.HasValue && dtos.Any())
            {
                var commentIds = dtos.Select(d => d.ForumCommentId).ToList();
                var votedIds = await _voteRepository.GetVotedCommentIdsByUserAsync(currentUserId.Value, commentIds);
                var votedSet = new HashSet<int>(votedIds);
                foreach (var dto in dtos)
                {
                    dto.IsUpvoted = votedSet.Contains(dto.ForumCommentId);
                }
            }

            return new PagedResult<ForumCommentResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ForumCommentResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize, int? currentUserId = null)
        {
            var paged = await _repository.GetByUserIdPagedAsync(userId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ForumCommentResponse>>(paged.Items);

            if (currentUserId.HasValue && dtos.Any())
            {
                var commentIds = dtos.Select(d => d.ForumCommentId).ToList();
                var votedIds = await _voteRepository.GetVotedCommentIdsByUserAsync(currentUserId.Value, commentIds);
                var votedSet = new HashSet<int>(votedIds);
                foreach (var dto in dtos)
                {
                    dto.IsUpvoted = votedSet.Contains(dto.ForumCommentId);
                }
            }

            return new PagedResult<ForumCommentResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ForumCommentResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ForumCommentResponse?> GetByIdAsync(int id, int? currentUserId = null)
        {
            var item = (await _repository.GetAllAsync(x => x.ForumCommentId == id, includes: x => x.User!)).FirstOrDefault();
            if (item == null) return null;

            var dto = _mapper.Map<ForumCommentResponse>(item);
            if (currentUserId.HasValue)
            {
                dto.IsUpvoted = await _voteRepository.IsCommentVotedAsync(id, currentUserId.Value);
            }

            return dto;
        }

        public async Task<ForumCommentResponse> CreateAsync(ForumCommentCreateRequest request, int userId)
        {
            var item = _mapper.Map<ForumComment>(request);
            item.UserId = userId;
            item.UpvoteCount = 0;
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = (await _repository.GetAllAsync(x => x.ForumCommentId == item.ForumCommentId, includes: x => x.User!)).FirstOrDefault();
            return _mapper.Map<ForumCommentResponse>(created ?? item);
        }

        public async Task<ForumCommentResponse?> UpdateAsync(int id, ForumCommentUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            item.UpdatedAt = DateTime.UtcNow;
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = (await _repository.GetAllAsync(x => x.ForumCommentId == id, includes: x => x.User!)).FirstOrDefault();
            return _mapper.Map<ForumCommentResponse>(updated ?? item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<CommentVoteToggleResponse> ToggleVoteAsync(int commentId, int currentUserId)
        {
            var comment = await _repository.GetByIdAsync(commentId);
            if (comment == null)
            {
                throw new KeyNotFoundException($"Forum comment with ID {commentId} does not exist.");
            }

            var (isUpvoted, upvoteCount) = await _voteRepository.ToggleVoteAsync(commentId, currentUserId);

            // Tự động tạo Notification cho tác giả bình luận khi có người upvote (nếu không phải tự upvote chính mình)
            if (isUpvoted && comment.UserId.HasValue && comment.UserId.Value != currentUserId)
            {
                try
                {
                    var voter = await _userRepository.GetByIdAsync(currentUserId);
                    var voterName = !string.IsNullOrWhiteSpace(voter?.FullName) ? voter.FullName : "Một người dùng";
                    var snippet = !string.IsNullOrWhiteSpace(comment.Content)
                        ? (comment.Content.Length > 40 ? comment.Content.Substring(0, 40) + "..." : comment.Content)
                        : "bình luận";

                    var notification = new Notification
                    {
                        UserId = comment.UserId.Value,
                        Message = $"[Forum] {voterName} đã ủng hộ bình luận của bạn: \"{snippet}\"",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationRepository.AddAsync(notification);
                    await _notificationRepository.SaveChangesAsync();
                }
                catch
                {
                    // Tránh lỗi notification làm gián đoạn luồng vote
                }
            }

            return new CommentVoteToggleResponse
            {
                ForumCommentId = commentId,
                UpvoteCount = upvoteCount,
                IsUpvoted = isUpvoted
            };
        }

        public async Task<List<int>> GetMyVotedCommentIdsAsync(int currentUserId)
        {
            return await _voteRepository.GetAllVotedCommentIdsByUserAsync(currentUserId);
        }
    }
}
