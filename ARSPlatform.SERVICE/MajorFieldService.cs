using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class MajorFieldService : IMajorFieldService
    {
        private readonly IMajorFieldRepository _repository;
        private readonly IMapper _mapper;

        public MajorFieldService(IMajorFieldRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MajorFieldResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllWithSubFieldsAsync();
            return _mapper.Map<IEnumerable<MajorFieldResponse>>(items);
        }

        public async Task<MajorFieldResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdWithSubFieldsAsync(id);
            return item == null ? null : _mapper.Map<MajorFieldResponse>(item);
        }

        public async Task<MajorFieldResponse> CreateAsync(MajorFieldCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Major field name is required.");
            }

            var normalizedName = request.Name.Trim();
            var exists = await _repository.ExistsAsync(x => x.Name == normalizedName);
            if (exists)
            {
                throw new InvalidOperationException("A major field with the same name already exists.");
            }

            var item = _mapper.Map<MajorField>(request);
            item.Name = normalizedName;
            item.CreatedAt ??= DateTime.UtcNow;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithSubFieldsAsync(item.MajorFieldId);
            return _mapper.Map<MajorFieldResponse>(created);
        }

        public async Task<MajorFieldResponse?> UpdateAsync(int id, MajorFieldUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Major field name is required.");
            }

            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            var normalizedName = request.Name.Trim();
            var duplicate = await _repository.ExistsAsync(x => x.MajorFieldId != id && x.Name == normalizedName);
            if (duplicate)
            {
                throw new InvalidOperationException("A major field with the same name already exists.");
            }

            _mapper.Map(request, item);
            item.Name = normalizedName;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithSubFieldsAsync(id);
            return _mapper.Map<MajorFieldResponse>(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            if (await _repository.HasSubFieldsAsync(id))
            {
                throw new InvalidOperationException("The major field cannot be deleted while it still contains sub-fields.");
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
