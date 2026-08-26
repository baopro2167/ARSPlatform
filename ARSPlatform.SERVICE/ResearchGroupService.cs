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
    public class ResearchGroupService : IResearchGroupService
    {
        private readonly IResearchGroupRepository _repository;
        private readonly IMapper _mapper;

        public ResearchGroupService(IResearchGroupRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ResearchGroupResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ResearchGroupResponse>>(items);
        }

        public async Task<PagedResult<ResearchGroupResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<ResearchGroupResponse>>(paged.Items);
            return new PagedResult<ResearchGroupResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ResearchGroupResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByLecturerIdPagedAsync(lecturerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ResearchGroupResponse>>(paged.Items);
            return new PagedResult<ResearchGroupResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ResearchGroupResponse>> GetByTopicIdAsync(int topicId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByTopicIdPagedAsync(topicId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ResearchGroupResponse>>(paged.Items);
            return new PagedResult<ResearchGroupResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ResearchGroupResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ResearchGroupResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<ResearchGroupResponse>(item);
        }

        public async Task<ResearchGroupResponse> CreateAsync(ResearchGroupCreateRequest request)
        {
            var item = _mapper.Map<ResearchGroup>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<ResearchGroupResponse>(item);
        }

        public async Task<ResearchGroupResponse?> UpdateAsync(int id, ResearchGroupUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<ResearchGroupResponse>(item);
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
