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
    public class MembershipPackageService : IMembershipPackageService
    {
        private readonly IMembershipPackageRepository _repository;
        private readonly IMapper _mapper;

        public MembershipPackageService(IMembershipPackageRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MembershipPackageResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<MembershipPackageResponse>>(items);
        }

        public async Task<PagedResult<MembershipPackageResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<MembershipPackageResponse>>(paged.Items);
            return new PagedResult<MembershipPackageResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<MembershipPackageResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<MembershipPackageResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<MembershipPackageResponse>(item);
        }

        public async Task<MembershipPackageResponse> CreateAsync(MembershipPackageCreateRequest request)
        {
            var item = _mapper.Map<MembershipPackage>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<MembershipPackageResponse>(item);
        }

        public async Task<MembershipPackageResponse?> UpdateAsync(int id, MembershipPackageUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<MembershipPackageResponse>(item);
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
