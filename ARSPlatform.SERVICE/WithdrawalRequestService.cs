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
    public class WithdrawalRequestService : IWithdrawalRequestService
    {
        private readonly IWithdrawalRequestRepository _repository;
        private readonly IMapper _mapper;

        public WithdrawalRequestService(IWithdrawalRequestRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<WithdrawalRequestResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<WithdrawalRequestResponse>>(items);
        }

        public async Task<WithdrawalRequestResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<WithdrawalRequestResponse>(item);
        }

        public async Task<WithdrawalRequestResponse> CreateAsync(WithdrawalRequestCreateRequest request)
        {
            var item = _mapper.Map<WithdrawalRequest>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<WithdrawalRequestResponse>(item);
        }
    }
}
