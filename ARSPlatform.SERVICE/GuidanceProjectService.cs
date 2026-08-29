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
    public class GuidanceProjectService : IGuidanceProjectService
    {
        private readonly IGuidanceProjectRepository _repository;
        private readonly IMapper _mapper;

        public GuidanceProjectService(IGuidanceProjectRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<GuidanceProjectResponse>> GetAllAsync(int? researchGroupId = null)
        {
            Expression<Func<GuidanceProject, bool>>? predicate = researchGroupId.HasValue
                ? x => x.ResearchGroupId == researchGroupId.Value
                : null;

            var items = await _repository.GetAllAsync(predicate, includes: new Expression<Func<GuidanceProject, object>>[]
            {
                x => x.Lecturer!,
                x => x.Student!,
                x => x.ResearchGroup!
            });
            return _mapper.Map<IEnumerable<GuidanceProjectResponse>>(items);
        }

        public async Task<PagedResult<GuidanceProjectResponse>> GetPagedAsync(PaginationParams paginationParams, int? researchGroupId = null)
        {
            Expression<Func<GuidanceProject, bool>>? predicate = researchGroupId.HasValue
                ? x => x.ResearchGroupId == researchGroupId.Value
                : null;

            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new Expression<Func<GuidanceProject, object>>[]
                {
                    x => x.Lecturer!,
                    x => x.Student!,
                    x => x.ResearchGroup!
                });
            var dtos = _mapper.Map<List<GuidanceProjectResponse>>(paged.Items);
            return new PagedResult<GuidanceProjectResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<GuidanceProjectResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByLecturerIdPagedAsync(lecturerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<GuidanceProjectResponse>>(paged.Items);
            return new PagedResult<GuidanceProjectResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<GuidanceProjectResponse>> GetByStudentIdAsync(int studentId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByStudentIdPagedAsync(studentId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<GuidanceProjectResponse>>(paged.Items);
            return new PagedResult<GuidanceProjectResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<GuidanceProjectResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<GuidanceProjectResponse?> GetByIdAsync(int id)
        {
            var item = (await _repository.GetAllAsync(x => x.GuidanceProjectId == id,
                x => x.Lecturer!,
                x => x.Student!,
                x => x.ResearchGroup!)).FirstOrDefault();
            return item == null ? null : _mapper.Map<GuidanceProjectResponse>(item);
        }

        public async Task<GuidanceProjectResponse> CreateAsync(GuidanceProjectCreateRequest request, int? lecturerId = null)
        {
            var item = _mapper.Map<GuidanceProject>(request);
            if (lecturerId.HasValue && !item.LecturerId.HasValue)
            {
                item.LecturerId = lecturerId.Value;
            }
            item.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            var created = await GetByIdAsync(item.GuidanceProjectId);
            return created ?? _mapper.Map<GuidanceProjectResponse>(item);
        }

        public async Task<GuidanceProjectResponse?> UpdateAsync(int id, GuidanceProjectUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<GuidanceProjectResponse>(item);
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
