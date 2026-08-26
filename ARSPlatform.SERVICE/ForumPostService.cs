using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class ForumPostService : IForumPostService
    {
        private readonly IForumPostRepository _repository;
        private readonly IMapper _mapper;

        public ForumPostService(IForumPostRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ForumPostResponse>> GetAllAsync(string? category = null, string? sort = null, string? search = null)
        {
            var items = await _repository.SearchAsync(search, category, sort);
            return _mapper.Map<IEnumerable<ForumPostResponse>>(items);
        }

        public async Task<ForumPostResponse?> GetByIdAsync(int id)
        {
            var item = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.ForumComments)
                .FirstOrDefaultAsync(p => p.ForumPostId == id);

            return item == null ? null : _mapper.Map<ForumPostResponse>(item);
        }

        public async Task<ForumPostResponse> CreateAsync(ForumPostCreateRequest request, int userId)
        {
            var item = _mapper.Map<ForumPost>(request);
            item.UserId = userId;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var createdItem = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.ForumComments)
                .FirstOrDefaultAsync(p => p.ForumPostId == item.ForumPostId);

            if (createdItem == null)
            {
                throw new System.Exception("Forum post was created but could not be loaded.");
            }

            return _mapper.Map<ForumPostResponse>(createdItem);
        }
    }
}
