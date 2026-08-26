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

        public async Task<CommentVoteResponse> CreateAsync(CommentVoteCreateRequest request)
        {
            var item = _mapper.Map<CommentVote>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<CommentVoteResponse>(item);
        }
    }
}
