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
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _repository;
        private readonly IMapper _mapper;

        public TransactionService(ITransactionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TransactionResponse>> GetAllAsync(int? walletId = null)
        {
            Expression<Func<Transaction, bool>>? predicate = walletId.HasValue ? x => x.WalletId == walletId.Value : null;
            var items = await _repository.GetAllAsync(predicate);
            return _mapper.Map<IEnumerable<TransactionResponse>>(items);
        }

        public async Task<PagedResult<TransactionResponse>> GetPagedAsync(PaginationParams paginationParams, int? walletId = null)
        {
            Expression<Func<Transaction, bool>>? predicate = walletId.HasValue ? x => x.WalletId == walletId.Value : null;
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt));
            var dtos = _mapper.Map<List<TransactionResponse>>(paged.Items);
            return new PagedResult<TransactionResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<TransactionResponse>> GetByWalletIdAsync(int walletId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByWalletIdPagedAsync(walletId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<TransactionResponse>>(paged.Items);
            return new PagedResult<TransactionResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<TransactionResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<TransactionResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<TransactionResponse>(item);
        }

        public async Task<TransactionResponse> CreateAsync(TransactionCreateRequest request)
        {
            var item = _mapper.Map<Transaction>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<TransactionResponse>(item);
        }

        public async Task<TransactionResponse?> UpdateAsync(int id, TransactionUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<TransactionResponse>(item);
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
