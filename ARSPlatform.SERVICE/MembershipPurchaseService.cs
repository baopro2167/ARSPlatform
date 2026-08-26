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
    public class MembershipPurchaseService : IMembershipPurchaseService
    {
        private readonly IMembershipPurchaseRepository _repository;
        private readonly IMapper _mapper;

        public MembershipPurchaseService(IMembershipPurchaseRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MembershipPurchaseResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<MembershipPurchaseResponse>>(items);
        }

        public async Task<PagedResult<MembershipPurchaseResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<MembershipPurchaseResponse>>(paged.Items);
            return new PagedResult<MembershipPurchaseResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<MembershipPurchaseResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByUserIdPagedAsync(userId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<MembershipPurchaseResponse>>(paged.Items);
            return new PagedResult<MembershipPurchaseResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<MembershipPurchaseResponse>> GetByPackageIdAsync(int packageId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByPackageIdPagedAsync(packageId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<MembershipPurchaseResponse>>(paged.Items);
            return new PagedResult<MembershipPurchaseResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<MembershipPurchaseResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<MembershipPurchaseResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<MembershipPurchaseResponse>(item);
        }

        public async Task<MembershipPurchaseResponse> CreateAsync(MembershipPurchaseCreateRequest request)
        {
            var item = _mapper.Map<MembershipPurchase>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<MembershipPurchaseResponse>(item);
        }

        public async Task<MembershipPurchaseResponse?> UpdateAsync(int id, MembershipPurchaseUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<MembershipPurchaseResponse>(item);
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
