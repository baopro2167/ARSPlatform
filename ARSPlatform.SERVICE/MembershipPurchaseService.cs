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
    public class MembershipPurchaseService : IMembershipPurchaseService
    {
        private readonly IMembershipPurchaseRepository _repository;
        private readonly IMapper _mapper;

        public MembershipPurchaseService(IMembershipPurchaseRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MembershipPurchaseResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<MembershipPurchaseResponse>>(items);
        }

        public async Task<MembershipPurchaseResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<MembershipPurchaseResponse>(item);
        }

        public async Task<MembershipPurchaseResponse> CreateAsync(MembershipPurchaseCreateRequest request)
        {
            var item = _mapper.Map<MembershipPurchase>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<MembershipPurchaseResponse>(item);
        }

        public async Task<MembershipPurchaseResponse?> UpdateAsync(int id, MembershipPurchaseUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<MembershipPurchaseResponse>(item);
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
