using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class LearningMaterialService : ILearningMaterialService
    {
        private readonly ILearningMaterialRepository _repository;
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public LearningMaterialService(
            ILearningMaterialRepository repository,
            AppDbContext dbContext,
            IMapper mapper)
        {
            _repository = repository;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LearningMaterialResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<LearningMaterialResponse>>(items);
        }

        public async Task<PagedResult<LearningMaterialResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<LearningMaterialResponse>>(paged.Items);
            return new PagedResult<LearningMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<LearningMaterialResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByLecturerIdPagedAsync(lecturerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<LearningMaterialResponse>>(paged.Items);
            return new PagedResult<LearningMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<LearningMaterialResponse>> GetBySubFieldIdAsync(int subFieldId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetBySubFieldIdPagedAsync(subFieldId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<LearningMaterialResponse>>(paged.Items);
            return new PagedResult<LearningMaterialResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<LearningMaterialResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<LearningMaterialResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<LearningMaterialResponse>(item);
        }

        public async Task<LearningMaterialResponse> CreateAsync(LearningMaterialCreateRequest request)
        {
            var item = _mapper.Map<LearningMaterial>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<LearningMaterialResponse>(item);
        }

        public async Task<LearningMaterialResponse?> UpdateAsync(int id, LearningMaterialUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<LearningMaterialResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            // 1. Usage Constraint Check: Kiểm tra xem tài liệu có đang được liên kết trong ResearchTopic hoặc PhasedReport
            var idString = id.ToString();
            var hasFileUrl = !string.IsNullOrWhiteSpace(item.FileUrl);

            // Kiểm tra ResearchTopic (thông qua GuidanceProjectsUrl)
            var isUsedInTopic = await _dbContext.ResearchTopics.AnyAsync(t =>
                t.GuidanceProjectsUrl != null &&
                (
                    (hasFileUrl && t.GuidanceProjectsUrl.Contains(item.FileUrl!)) ||
                    t.GuidanceProjectsUrl == idString ||
                    t.GuidanceProjectsUrl.Contains($"/materials/{id}") ||
                    t.GuidanceProjectsUrl.Contains($"/learning-materials/{id}") ||
                    t.GuidanceProjectsUrl.Contains($"materialId={id}")
                ));

            if (isUsedInTopic)
            {
                throw new InvalidOperationException("Tài liệu đang được sử dụng trong đề tài nghiên cứu, không thể xóa.");
            }

            // Kiểm tra PhasedReport (thông qua PhasedMaterialsUrl)
            var isUsedInPhasedReport = await _dbContext.PhasedReports.AnyAsync(p =>
                p.PhasedMaterialsUrl != null &&
                (
                    (hasFileUrl && p.PhasedMaterialsUrl.Contains(item.FileUrl!)) ||
                    p.PhasedMaterialsUrl == idString ||
                    p.PhasedMaterialsUrl.Contains($"/materials/{id}") ||
                    p.PhasedMaterialsUrl.Contains($"/learning-materials/{id}") ||
                    p.PhasedMaterialsUrl.Contains($"materialId={id}")
                ));

            if (isUsedInPhasedReport)
            {
                throw new InvalidOperationException("Tài liệu đang được sử dụng trong đề tài nghiên cứu, không thể xóa.");
            }

            // 2. Cascade Delete: Xóa toàn bộ các bản ghi trong SharedMaterials liên kết tới tài liệu này
            var relatedShares = await _dbContext.SharedMaterials
                .Where(s => s.LearningMaterialId == id || s.PaperId == id)
                .ToListAsync();

            if (relatedShares.Any())
            {
                _dbContext.SharedMaterials.RemoveRange(relatedShares);
            }

            // 3. Xóa tài liệu gốc
            _repository.Delete(item);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
