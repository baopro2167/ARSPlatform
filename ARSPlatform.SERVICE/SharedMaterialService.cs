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
    public class SharedMaterialService : ISharedMaterialService
    {
        private readonly ISharedMaterialRepository _repository;
        private readonly IMapper _mapper;

        public SharedMaterialService(ISharedMaterialRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SharedMaterialResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<SharedMaterialResponse>>(items);
        }

        public async Task<PagedResult<SharedMaterialResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<SharedMaterialResponse>>(paged.Items);
            return new PagedResult<SharedMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SharedMaterialResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByLecturerIdPagedAsync(lecturerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<SharedMaterialResponse>>(paged.Items);
            return new PagedResult<SharedMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SharedMaterialResponse>> GetByPaperIdAsync(int paperId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByPaperIdPagedAsync(paperId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<SharedMaterialResponse>>(paged.Items);
            return new PagedResult<SharedMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SharedMaterialResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<SharedMaterialResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<SharedMaterialResponse>(item);
        }

        public async Task<SharedMaterialResponse> CreateAsync(SharedMaterialCreateRequest request)
        {
            var item = _mapper.Map<SharedMaterial>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<SharedMaterialResponse>(item);
        }

        public async Task<SharedMaterialResponse?> UpdateAsync(int id, SharedMaterialUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<SharedMaterialResponse>(item);
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
