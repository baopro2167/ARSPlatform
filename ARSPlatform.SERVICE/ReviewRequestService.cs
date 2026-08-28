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
    public class ReviewRequestService : IReviewRequestService
    {
        private readonly IReviewRequestRepository _repository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public ReviewRequestService(
            IReviewRequestRepository repository,
            INotificationRepository notificationRepository,
            IMapper mapper)
        {
            _repository = repository;
            _notificationRepository = notificationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReviewRequestResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllWithReviewerAsync();
            return _mapper.Map<IEnumerable<ReviewRequestResponse>>(items);
        }

        public async Task<PagedResult<ReviewRequestResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                includes: new System.Linq.Expressions.Expression<System.Func<ReviewRequest, object>>[]
                {
                    x => x.Reviewer!,
                    x => x.Paper!
                });
            var dtos = _mapper.Map<List<ReviewRequestResponse>>(paged.Items);
            return new PagedResult<ReviewRequestResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ReviewRequestResponse>> GetByReviewerIdAsync(int reviewerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByReviewerIdPagedAsync(reviewerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ReviewRequestResponse>>(paged.Items);
            return new PagedResult<ReviewRequestResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ReviewRequestResponse>> GetByPaperIdAsync(int paperId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByPaperIdPagedAsync(paperId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ReviewRequestResponse>>(paged.Items);
            return new PagedResult<ReviewRequestResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ReviewRequestResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ReviewRequestResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdWithReviewerAsync(id);
            return item == null ? null : _mapper.Map<ReviewRequestResponse>(item);
        }

        public async Task<ReviewRequestResponse> CreateAsync(ReviewRequestCreateRequest request)
        {
            var item = _mapper.Map<ReviewRequest>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithReviewerAsync(item.ReviewRequestId);

            if (created != null && created.ReviewerId.HasValue)
            {
                var paperTitle = created.Paper?.Title ?? "Bài báo mới";
                var notification = new Notification
                {
                    UserId = created.ReviewerId.Value,
                    Message = $"Bạn có một bài báo mới được phân công phản biện: \"{paperTitle}\".",
                    IsRead = false,
                    CreatedAt = System.DateTime.UtcNow
                };
                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }

            return _mapper.Map<ReviewRequestResponse>(created ?? item);
        }

        public async Task<ReviewRequestResponse?> UpdateAsync(int id, ReviewRequestUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithReviewerAsync(id);
            return _mapper.Map<ReviewRequestResponse>(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
