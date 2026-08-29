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
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repository;
        private readonly IMapper _mapper;

        public ReportService(IReportRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReportResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ReportResponse>>(items);
        }

        public async Task<PagedResult<ReportResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<ReportResponse>>(paged.Items);
            return new PagedResult<ReportResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ReportResponse>> GetByReporterIdAsync(int reporterId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByReporterIdPagedAsync(reporterId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ReportResponse>>(paged.Items);
            return new PagedResult<ReportResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ReportResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ReportResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<ReportResponse>(item);
        }

        public async Task<ReportResponse> CreateAsync(ReportCreateRequest request)
        {
            var item = _mapper.Map<Report>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<ReportResponse>(item);
        }

        public async Task<ReportResponse?> UpdateAsync(int id, ReportUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<ReportResponse>(item);
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
