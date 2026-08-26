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
    public class PhasedReportService : IPhasedReportService
    {
        private readonly IPhasedReportRepository _repository;
        private readonly IMapper _mapper;

        public PhasedReportService(IPhasedReportRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PhasedReportResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<PhasedReportResponse>>(items);
        }

        public async Task<PagedResult<PhasedReportResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<PhasedReportResponse>>(paged.Items);
            return new PagedResult<PhasedReportResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<PhasedReportResponse>> GetByResearchGroupIdAsync(int researchGroupId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByResearchGroupIdPagedAsync(researchGroupId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<PhasedReportResponse>>(paged.Items);
            return new PagedResult<PhasedReportResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<PhasedReportResponse>> GetByGroupMemberIdAsync(int groupMemberId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByGroupMemberIdPagedAsync(groupMemberId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<PhasedReportResponse>>(paged.Items);
            return new PagedResult<PhasedReportResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<PhasedReportResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PhasedReportResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<PhasedReportResponse>(item);
        }

        public async Task<PhasedReportResponse> CreateAsync(PhasedReportCreateRequest request)
        {
            var item = _mapper.Map<PhasedReport>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<PhasedReportResponse>(item);
        }

        public async Task<PhasedReportResponse?> UpdateAsync(int id, PhasedReportUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<PhasedReportResponse>(item);
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
