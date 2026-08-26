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
    public class GuidanceProjectService : IGuidanceProjectService
    {
        private readonly IGuidanceProjectRepository _repository;
        private readonly IMapper _mapper;

        public GuidanceProjectService(IGuidanceProjectRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<GuidanceProjectResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<GuidanceProjectResponse>>(items);
        }

        public async Task<GuidanceProjectResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<GuidanceProjectResponse>(item);
        }

        public async Task<GuidanceProjectResponse> CreateAsync(GuidanceProjectCreateRequest request)
        {
            var item = _mapper.Map<GuidanceProject>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<GuidanceProjectResponse>(item);
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
