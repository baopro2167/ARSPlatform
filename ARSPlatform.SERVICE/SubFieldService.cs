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
    public class SubFieldService : ISubFieldService
    {
        private readonly ISubFieldRepository _repository;
        private readonly IMajorFieldRepository _majorFieldRepository;
        private readonly IMapper _mapper;

        public SubFieldService(
            ISubFieldRepository repository,
            IMajorFieldRepository majorFieldRepository,
            IMapper mapper)
        {
            _repository = repository;
            _majorFieldRepository = majorFieldRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SubFieldResponse>> GetAllAsync(int? majorFieldId = null)
        {
            if (majorFieldId.HasValue && majorFieldId.Value <= 0)
            {
                throw new ArgumentException("majorFieldId must be greater than zero.");
            }

            if (majorFieldId.HasValue && !await _majorFieldRepository.ExistsAsync(x => x.MajorFieldId == majorFieldId.Value))
            {
                throw new KeyNotFoundException("Major field not found.");
            }

            var items = await _repository.GetAllWithMajorFieldAsync(majorFieldId);
            return _mapper.Map<IEnumerable<SubFieldResponse>>(items);
        }

        public async Task<PagedResult<SubFieldResponse>> GetPagedAsync(PaginationParams paginationParams, int? majorFieldId = null)
        {
            if (majorFieldId.HasValue && majorFieldId.Value <= 0)
            {
                throw new ArgumentException("majorFieldId must be greater than zero.");
            }

            if (majorFieldId.HasValue && !await _majorFieldRepository.ExistsAsync(x => x.MajorFieldId == majorFieldId.Value))
            {
                throw new KeyNotFoundException("Major field not found.");
            }

            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: majorFieldId.HasValue ? x => x.MajorFieldId == majorFieldId.Value : null,
                includes: x => x.MajorField!);

            var dtos = _mapper.Map<List<SubFieldResponse>>(paged.Items);
            return new PagedResult<SubFieldResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<SubFieldResponse>> GetByMajorFieldIdAsync(int majorFieldId, int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize }, majorFieldId);
        }

        public async Task<PagedResult<SubFieldResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<SubFieldResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdWithMajorFieldAsync(id);
            return item == null ? null : _mapper.Map<SubFieldResponse>(item);
        }

        public async Task<SubFieldResponse> CreateAsync(SubFieldCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Sub-field name is required.");
            }

            if (!request.MajorFieldId.HasValue || request.MajorFieldId.Value <= 0)
            {
                throw new ArgumentException("MajorFieldId is required.");
            }

            var majorFieldExists = await _majorFieldRepository.ExistsAsync(x => x.MajorFieldId == request.MajorFieldId.Value);
            if (!majorFieldExists)
            {
                throw new ArgumentException("The specified major field does not exist.");
            }

            var normalizedName = request.Name.Trim();
            var duplicate = await _repository.ExistsAsync(x => x.MajorFieldId == request.MajorFieldId.Value && x.Name == normalizedName);
            if (duplicate)
            {
                throw new InvalidOperationException("A sub-field with the same name already exists under this major field.");
            }

            var item = _mapper.Map<SubField>(request);
            item.Name = normalizedName;
            item.CreatedAt ??= DateTime.UtcNow;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithMajorFieldAsync(item.SubFieldId);
            return _mapper.Map<SubFieldResponse>(created);
        }

        public async Task<SubFieldResponse?> UpdateAsync(int id, SubFieldUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Sub-field name is required.");
            }

            if (!request.MajorFieldId.HasValue || request.MajorFieldId.Value <= 0)
            {
                throw new ArgumentException("MajorFieldId is required.");
            }

            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            var majorFieldExists = await _majorFieldRepository.ExistsAsync(x => x.MajorFieldId == request.MajorFieldId.Value);
            if (!majorFieldExists)
            {
                throw new ArgumentException("The specified major field does not exist.");
            }

            if (item.MajorFieldId != request.MajorFieldId.Value && await _repository.HasUsageAsync(id))
            {
                throw new InvalidOperationException("The sub-field cannot be moved to another major field while it is referenced by professional profiles, papers, or learning materials.");
            }

            var normalizedName = request.Name.Trim();
            var duplicate = await _repository.ExistsAsync(x => x.SubFieldId != id && x.MajorFieldId == request.MajorFieldId.Value && x.Name == normalizedName);
            if (duplicate)
            {
                throw new InvalidOperationException("A sub-field with the same name already exists under this major field.");
            }

            _mapper.Map(request, item);
            item.Name = normalizedName;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithMajorFieldAsync(id);
            return _mapper.Map<SubFieldResponse>(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            if (await _repository.HasUsageAsync(id))
            {
                throw new InvalidOperationException("The sub-field cannot be deleted because it is referenced by professional profiles, papers, or learning materials.");
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
