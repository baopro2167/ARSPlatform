using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using ProfileEntity = ARSPlatform.MODEL.Entities.Profile;

namespace ARSPlatform.SERVICES
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _repository;
        private readonly IMapper _mapper;

        public ProfileService(IProfileRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProfileResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllWithUserAsync();
            return _mapper.Map<IEnumerable<ProfileResponse>>(items);
        }

        public async Task<ProfileResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdWithUserAsync(id);
            return item == null ? null : _mapper.Map<ProfileResponse>(item);
        }

        public async Task<ProfileResponse> CreateAsync(ProfileCreateRequest request)
        {
            var existing = await _repository.GetByIdAsync(request.UserId);
            if (existing != null)
            {
                throw new InvalidOperationException("A profile already exists for this user.");
            }

            var item = _mapper.Map<ProfileEntity>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithUserAsync(item.UserId);
            return _mapper.Map<ProfileResponse>(created);
        }

        public async Task<ProfileResponse?> UpdateAsync(int id, ProfileUpdateRequest request)
        {
            if (request.UserId != id)
            {
                throw new ArgumentException("The request UserId must match the route id.");
            }

            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithUserAsync(id);
            return _mapper.Map<ProfileResponse>(updated);
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
