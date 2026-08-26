using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
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

        public async Task<PagedResult<FollowerResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<FollowerResponse>>(paged.Items);
            return new PagedResult<FollowerResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<FollowerResponse>> GetByFollowedIdAsync(int followedId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByFollowedIdPagedAsync(followedId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<FollowerResponse>>(paged.Items);
            return new PagedResult<FollowerResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<FollowerResponse>> GetByFollowerIdAsync(int followerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByFollowerIdPagedAsync(followerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<FollowerResponse>>(paged.Items);
            return new PagedResult<FollowerResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<FollowerResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
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
