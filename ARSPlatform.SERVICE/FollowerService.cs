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
    public class FollowerService : IFollowerService
    {
        private readonly IFollowerRepository _repository;
        private readonly IMapper _mapper;

        public FollowerService(IFollowerRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<FollowerResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<FollowerResponse>>(items);
        }

        public async Task<FollowerResponse> CreateAsync(FollowerCreateRequest request)
        {
            var item = _mapper.Map<Follower>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<FollowerResponse>(item);
        }
    }
}
