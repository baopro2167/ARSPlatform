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
    public class UserTokenService : IUserTokenService
    {
        private readonly IUserTokenRepository _repository;
        private readonly IMapper _mapper;

        public UserTokenService(IUserTokenRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserTokenResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserTokenResponse>>(items);
        }

        public async Task<UserTokenResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<UserTokenResponse>(item);
        }

        public async Task<UserTokenResponse> CreateAsync(UserTokenCreateRequest request)
        {
            var item = _mapper.Map<UserToken>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<UserTokenResponse>(item);
        }

        public async Task<UserTokenResponse?> UpdateAsync(int id, UserTokenUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<UserTokenResponse>(item);
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
