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
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IMapper _mapper;

        public NotificationService(INotificationRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<NotificationResponse>> GetAllAsync(int? userId = null)
        {
            Expression<Func<Notification, bool>>? predicate = userId.HasValue ? x => x.UserId == userId.Value : null;
            var items = await _repository.GetAllAsync(predicate);
            return _mapper.Map<IEnumerable<NotificationResponse>>(items);
        }

        public async Task<PagedResult<NotificationResponse>> GetPagedAsync(PaginationParams paginationParams, int? userId = null)
        {
            Expression<Func<Notification, bool>>? predicate = userId.HasValue ? x => x.UserId == userId.Value : null;
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt));
            var dtos = _mapper.Map<List<NotificationResponse>>(paged.Items);
            return new PagedResult<NotificationResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<NotificationResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByUserIdPagedAsync(userId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<NotificationResponse>>(paged.Items);
            return new PagedResult<NotificationResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<NotificationResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<NotificationResponse?> GetByIdAsync(int id, int? currentUserId = null, bool isAdmin = false)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            if (currentUserId.HasValue && !isAdmin && item.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this notification.");
            }

            return _mapper.Map<NotificationResponse>(item);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _repository.GetUnreadCountAsync(userId);
        }

        public async Task<NotificationResponse?> MarkAsReadAsync(int id, int currentUserId, bool isAdmin = false)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            if (!isAdmin && item.UserId != currentUserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this notification.");
            }

            item.IsRead = true;
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<NotificationResponse>(item);
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            return await _repository.MarkAllAsReadAsync(userId);
        }

        public async Task<NotificationResponse> CreateAsync(NotificationCreateRequest request)
        {
            var item = _mapper.Map<Notification>(request);
            item.CreatedAt = DateTime.UtcNow;
            if (item.IsRead == null) item.IsRead = false;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<NotificationResponse>(item);
        }

        public async Task<NotificationResponse> CreateNotificationAsync(int userId, string message)
        {
            var item = new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<NotificationResponse>(item);
        }

        public async Task<NotificationResponse?> UpdateAsync(int id, NotificationUpdateRequest request, int? currentUserId = null, bool isAdmin = false)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            if (currentUserId.HasValue && !isAdmin && item.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this notification.");
            }

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<NotificationResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id, int? currentUserId = null, bool isAdmin = false)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            if (currentUserId.HasValue && !isAdmin && item.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this notification.");
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
