using System;
using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;

namespace ARSPlatform.SERVICES
{
    public class UserRewardService : IUserRewardService
    {
        private readonly IUserRewardRepository _repository;
        private readonly IMapper _mapper;

        public UserRewardService(IUserRewardRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<UserRewardResponse>> GetPagedAsync(UserRewardPaginationRequest request)
        {
            var pp = new PaginationParams
            {
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize
            };
            var paged = await _repository.GetPagedAsync(pp, request.Status, request.Search);
            var dtos = _mapper.Map<System.Collections.Generic.List<UserRewardResponse>>(paged.Items);
            return new PagedResult<UserRewardResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<UserRewardResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<UserRewardResponse>(item);
        }

        public async Task<UserRewardResponse> CreateAsync(UserRewardCreateRequest request)
        {
            var entity = _mapper.Map<UserReward>(request);
            if (string.IsNullOrWhiteSpace(entity.Status))
            {
                entity.Status = "Active";
            }
            entity.UpdateAt = DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
            return _mapper.Map<UserRewardResponse>(entity);
        }

        public async Task<UserRewardResponse?> UpdateAsync(int id, UserRewardUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                item.Name = request.Name;
            }
            if (request.Description != null)
            {
                item.Description = request.Description;
            }
            if (request.RewardMonths.HasValue)
            {
                item.RewardMonths = request.RewardMonths.Value;
            }
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                item.Status = request.Status;
            }
            item.UpdateAt = DateTime.UtcNow;

            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<UserRewardResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<UserRewardResponse?> UpdateStatusAsync(int id, string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return null;
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            item.Status = status.Trim();
            item.UpdateAt = DateTime.UtcNow;

            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<UserRewardResponse>(item);
        }

        public async Task<UserRewardResponse?> FindActiveByNameContainsAsync(string keyword)
        {
            var item = await _repository.FindActiveByNameContainsAsync(keyword);
            return item == null ? null : _mapper.Map<UserRewardResponse>(item);
        }
    }
}
