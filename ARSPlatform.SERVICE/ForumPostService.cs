using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class ForumPostService : IForumPostService
    {
        private readonly IForumPostRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public ForumPostService(
            IForumPostRepository repository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IMapper mapper)
        {
            _repository = repository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ForumPostResponse>> GetAllAsync(string? category = null, string? sort = null, string? search = null, int? currentUserId = null)
        {
            var items = await _repository.SearchAsync(search, category, sort);
            var dtos = _mapper.Map<List<ForumPostResponse>>(items);

            if (currentUserId.HasValue && dtos.Any())
            {
                var postIds = dtos.Select(d => d.Id).ToList();
                var likedIds = await _repository.GetLikedPostIdsByUserAsync(currentUserId.Value, postIds);
                var likedSet = new HashSet<int>(likedIds);
                foreach (var dto in dtos)
                {
                    dto.IsLiked = likedSet.Contains(dto.Id);
                }
            }

            return dtos;
        }

        public async Task<PagedResult<ForumPostResponse>> GetPagedAsync(PaginationParams paginationParams, string? category = null, string? sort = null, string? search = null, int? currentUserId = null)
        {
            var paged = await _repository.SearchPagedAsync(paginationParams, search, category, sort);
            var dtos = _mapper.Map<List<ForumPostResponse>>(paged.Items);

            if (currentUserId.HasValue && dtos.Any())
            {
                var postIds = dtos.Select(d => d.Id).ToList();
                var likedIds = await _repository.GetLikedPostIdsByUserAsync(currentUserId.Value, postIds);
                var likedSet = new HashSet<int>(likedIds);
                foreach (var dto in dtos)
                {
                    dto.IsLiked = likedSet.Contains(dto.Id);
                }
            }

            return new PagedResult<ForumPostResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ForumPostResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ForumPostResponse?> GetByIdAsync(int id, int? currentUserId = null)
        {
            var item = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.ForumComments)
                .FirstOrDefaultAsync(p => p.ForumPostId == id);

            if (item == null) return null;

            var dto = _mapper.Map<ForumPostResponse>(item);
            if (currentUserId.HasValue)
            {
                dto.IsLiked = await _repository.IsPostLikedAsync(id, currentUserId.Value);
            }

            return dto;
        }

        public async Task<ForumPostResponse> CreateAsync(ForumPostCreateRequest request, int userId)
        {
            var item = _mapper.Map<ForumPost>(request);
            item.UserId = userId;

            // Tự động xử lý Title nếu người dùng không nhập tiêu đề
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                if (!string.IsNullOrWhiteSpace(item.Content))
                {
                    var cleanContent = item.Content.Trim();
                    item.Title = cleanContent.Length > 60
                        ? cleanContent.Substring(0, 60) + "..."
                        : cleanContent;
                }
                else
                {
                    item.Title = "General Post";
                }
            }

            var now = System.DateTime.UtcNow;
            item.CreatedAt = now;
            item.UpdatedAt = now;
            item.LikeCount = 0;
            item.ViewCount = 0;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var createdItem = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.ForumComments)
                .FirstOrDefaultAsync(p => p.ForumPostId == item.ForumPostId);

            if (createdItem == null)
            {
                throw new System.Exception("Forum post was created but could not be loaded.");
            }

            return _mapper.Map<ForumPostResponse>(createdItem);
        }

        public async Task<ForumPostLikeToggleResponse> ToggleLikeAsync(int postId, int currentUserId)
        {
            var post = await _repository.GetByIdAsync(postId);
            if (post == null)
                throw new KeyNotFoundException($"Forum post with ID {postId} does not exist.");

            var (isLiked, likesCount) = await _repository.ToggleLikeAsync(postId, currentUserId);

            // Tự động tạo Notification cho tác giả bài viết khi có người bấm thích (nếu không phải tự like bài mình)
            if (isLiked && post.UserId != currentUserId)
            {
                try
                {
                    var liker = await _userRepository.GetByIdAsync(currentUserId);
                    var likerName = !string.IsNullOrWhiteSpace(liker?.FullName) ? liker.FullName : "Một người dùng";
                    var postTitle = !string.IsNullOrWhiteSpace(post.Title)
                        ? (post.Title.Length > 50 ? post.Title.Substring(0, 50) + "..." : post.Title)
                        : "bài viết";

                    var notification = new Notification
                    {
                        UserId = post.UserId,
                        Message = $"[Forum] {likerName} đã thích bài viết của bạn: \"{postTitle}\"",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationRepository.AddAsync(notification);
                    await _notificationRepository.SaveChangesAsync();
                }
                catch
                {
                    // Tránh lỗi notification làm hỏng luồng Like
                }
            }

            return new ForumPostLikeToggleResponse
            {
                PostId = postId,
                Likes = likesCount,
                IsLiked = isLiked
            };
        }

        public async Task<List<int>> GetMyLikedPostIdsAsync(int currentUserId)
        {
            return await _repository.GetAllLikedPostIdsByUserAsync(currentUserId);
        }
    }
}
