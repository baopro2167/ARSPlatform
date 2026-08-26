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
    public class CommentVoteService : ICommentVoteService
    {
        private readonly ICommentVoteRepository _repository;
        private readonly IMapper _mapper;

        public CommentVoteService(ICommentVoteRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CommentVoteResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<CommentVoteResponse>>(items);
        }

        public async Task<PagedResult<CommentVoteResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams);
            var dtos = _mapper.Map<List<CommentVoteResponse>>(paged.Items);
            return new PagedResult<CommentVoteResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<CommentVoteResponse>> GetByCommentIdAsync(int commentId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByCommentIdPagedAsync(commentId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<CommentVoteResponse>>(paged.Items);
            return new PagedResult<CommentVoteResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<CommentVoteResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByUserIdPagedAsync(userId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<CommentVoteResponse>>(paged.Items);
            return new PagedResult<CommentVoteResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<CommentVoteResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<CommentVoteResponse> CreateAsync(CommentVoteCreateRequest request)
        {
            var item = _mapper.Map<CommentVote>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<CommentVoteResponse>(item);
        }
    }
}
