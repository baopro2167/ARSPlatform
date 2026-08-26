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
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _repository;
        private readonly IMapper _mapper;

        public WalletService(IWalletRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<WalletResponse>> GetAllAsync(int? userId = null)
        {
            if (userId.HasValue)
            {
                var item = await _repository.GetByUserIdAsync(userId.Value);
                if (item == null)
                {
                    return Array.Empty<WalletResponse>();
                }
                return new[] { _mapper.Map<WalletResponse>(item) };
            }

            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<WalletResponse>>(items);
        }

        public async Task<PagedResult<WalletResponse>> GetPagedAsync(PaginationParams paginationParams, int? userId = null)
        {
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: userId.HasValue ? x => x.UserId == userId.Value : null);
            var dtos = _mapper.Map<List<WalletResponse>>(paged.Items);
            return new PagedResult<WalletResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<WalletResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize }, userId);
        }

        public async Task<PagedResult<WalletResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<WalletResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<WalletResponse>(item);
        }

        public async Task<WalletResponse> CreateAsync(WalletCreateRequest request)
        {
            var item = _mapper.Map<Wallet>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<WalletResponse>(item);
        }

        public async Task<WalletResponse?> UpdateAsync(int id, WalletUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<WalletResponse>(item);
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
