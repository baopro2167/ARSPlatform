using System;
using System.Collections.Generic;
using System.Linq;
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
    public class FollowerService : IFollowerService
    {
        private readonly IFollowerRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public FollowerService(
            IFollowerRepository repository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IMapper mapper)
        {
            _repository = repository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<FollowerResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllWithUsersAsync();
            return _mapper.Map<IEnumerable<FollowerResponse>>(items);
        }

        public async Task<PagedResult<FollowerResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedWithUsersAsync(paginationParams);
            var dtos = _mapper.Map<List<FollowerResponse>>(paged.Items);
            return new PagedResult<FollowerResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<FollowerResponse>> GetByFollowedIdAsync(int followedId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByFollowedIdPagedAsync(followedId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<FollowerResponse>>(paged.Items);
            return new PagedResult<FollowerResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<FollowerResponse>> GetByFollowerIdAsync(int followerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByFollowerIdPagedAsync(followerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<FollowerResponse>>(paged.Items);
            return new PagedResult<FollowerResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<FollowerResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<FollowerResponse> CreateAsync(FollowerCreateRequest request, int currentUserId)
        {
            return await FollowAsync(currentUserId, request.FollowedId);
        }

        public async Task<FollowerResponse> FollowAsync(int currentUserId, int followedId)
        {
            if (currentUserId <= 0)
                throw new UnauthorizedAccessException("You must be logged in to follow a user.");

            if (currentUserId == followedId)
                throw new InvalidOperationException("You cannot follow yourself.");

            var targetUser = await _userRepository.GetByIdAsync(followedId);
            if (targetUser == null)
                throw new KeyNotFoundException($"User with ID {followedId} does not exist.");

            var existing = await _repository.GetRelationAsync(currentUserId, followedId);
            if (existing != null)
            {
                return _mapper.Map<FollowerResponse>(existing);
            }

            var item = new Follower
            {
                FollowerId = currentUserId,
                FollowedId = followedId,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            // Tự động tạo bản ghi thông báo cho Người B (người được follow)
            try
            {
                var followerUser = await _userRepository.GetByIdAsync(currentUserId);
                var followerName = !string.IsNullOrWhiteSpace(followerUser?.FullName) ? followerUser.FullName : "Một người dùng";
                var notification = new Notification
                {
                    UserId = followedId,
                    Message = $"{followerName} đã bắt đầu theo dõi bạn.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }
            catch
            {
                // Bỏ qua nếu có lỗi phát sinh để không làm gián đoạn luồng chính
            }

            var created = await _repository.GetRelationAsync(currentUserId, followedId);
            return _mapper.Map<FollowerResponse>(created ?? item);
        }

        public async Task<bool> UnfollowAsync(int currentUserId, int followedId)
        {
            if (currentUserId <= 0)
                throw new UnauthorizedAccessException("You must be logged in to unfollow a user.");

            var existing = await _repository.GetRelationAsync(currentUserId, followedId);
            if (existing == null)
                return false;

            _repository.Delete(existing);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleFollowAsync(int currentUserId, int followedId)
        {
            if (currentUserId <= 0)
                throw new UnauthorizedAccessException("You must be logged in to follow/unfollow.");

            if (currentUserId == followedId)
                throw new InvalidOperationException("You cannot follow yourself.");

            var existing = await _repository.GetRelationAsync(currentUserId, followedId);
            if (existing != null)
            {
                _repository.Delete(existing);
                await _repository.SaveChangesAsync();
                return false; // Now unfollowed
            }
            else
            {
                var targetUser = await _userRepository.GetByIdAsync(followedId);
                if (targetUser == null)
                    throw new KeyNotFoundException($"User with ID {followedId} does not exist.");

                var item = new Follower
                {
                    FollowerId = currentUserId,
                    FollowedId = followedId,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(item);
                await _repository.SaveChangesAsync();

                // Tự động tạo bản ghi thông báo cho Người B (người được follow)
                try
                {
                    var followerUser = await _userRepository.GetByIdAsync(currentUserId);
                    var followerName = !string.IsNullOrWhiteSpace(followerUser?.FullName) ? followerUser.FullName : "Một người dùng";
                    var notification = new Notification
                    {
                        UserId = followedId,
                        Message = $"{followerName} đã bắt đầu theo dõi bạn.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _notificationRepository.AddAsync(notification);
                    await _notificationRepository.SaveChangesAsync();
                }
                catch
                {
                    // Bỏ qua nếu có lỗi phát sinh để không làm gián đoạn luồng chính
                }

                return true; // Now following
            }
        }

        public async Task<bool> IsFollowingAsync(int currentUserId, int followedId)
        {
            if (currentUserId <= 0 || followedId <= 0)
                return false;

            return await _repository.IsFollowingAsync(currentUserId, followedId);
        }

        public async Task<FollowCountsResponse> GetCountsAsync(int userId)
        {
            var followersCount = await _repository.GetFollowersCountAsync(userId);
            var followingCount = await _repository.GetFollowingCountAsync(userId);

            return new FollowCountsResponse
            {
                UserId = userId,
                FollowersCount = followersCount,
                FollowingCount = followingCount
            };
        }
    }
}
