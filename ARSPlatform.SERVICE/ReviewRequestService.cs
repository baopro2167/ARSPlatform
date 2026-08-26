using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class ReviewRequestService : IReviewRequestService
    {
        private readonly IReviewRequestRepository _repository;
        private readonly IMapper _mapper;

        public ReviewRequestService(IReviewRequestRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReviewRequestResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllWithReviewerAsync();
            return _mapper.Map<IEnumerable<ReviewRequestResponse>>(items);
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
            return _mapper.Map<ReviewRequestResponse>(created);
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
