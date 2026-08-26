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
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _repository;
        private readonly IMapper _mapper;

        public WalletService(IWalletRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<WalletResponse>> GetAllAsync(int? userId = null)
        {
            if (userId.HasValue)
            {
                var item = await _repository.GetByUserIdAsync(userId.Value);
                if (item == null)
                {
                    return Array.Empty<WalletResponse>();
                }
                return new[] { _mapper.Map<WalletResponse>(item) };
            }

            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<WalletResponse>>(items);
        }

        public async Task<WalletResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<WalletResponse>(item);
        }

        public async Task<WalletResponse> CreateAsync(WalletCreateRequest request)
        {
            var item = _mapper.Map<Wallet>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<WalletResponse>(item);
        }

        public async Task<WalletResponse?> UpdateAsync(int id, WalletUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<WalletResponse>(item);
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
