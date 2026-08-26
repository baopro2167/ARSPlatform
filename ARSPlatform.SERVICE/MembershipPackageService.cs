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
    public class MembershipPackageService : IMembershipPackageService
    {
        private readonly IMembershipPackageRepository _repository;
        private readonly IMapper _mapper;

        public MembershipPackageService(IMembershipPackageRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MembershipPackageResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<MembershipPackageResponse>>(items);
        }

        public async Task<MembershipPackageResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<MembershipPackageResponse>(item);
        }

        public async Task<MembershipPackageResponse> CreateAsync(MembershipPackageCreateRequest request)
        {
            var item = _mapper.Map<MembershipPackage>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<MembershipPackageResponse>(item);
        }

        public async Task<MembershipPackageResponse?> UpdateAsync(int id, MembershipPackageUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<MembershipPackageResponse>(item);
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
