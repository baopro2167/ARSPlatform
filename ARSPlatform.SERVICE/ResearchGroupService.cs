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
