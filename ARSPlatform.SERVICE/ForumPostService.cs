using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
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

        public async Task<PagedResult<ForumPostResponse>> GetPagedAsync(PaginationParams paginationParams, string? category = null, string? sort = null, string? search = null)
        {
            var paged = await _repository.SearchPagedAsync(paginationParams, search, category, sort);
            var dtos = _mapper.Map<List<ForumPostResponse>>(paged.Items);
            return new PagedResult<ForumPostResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ForumPostResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
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

            // Tự động xử lý Title nếu người dùng không nhập tiêu đề
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                if (!string.IsNullOrWhiteSpace(item.Content))
                {
                    var cleanContent = item.Content.Trim();
                    item.Title = cleanContent.Length > 60
                        ? cleanContent.Substring(0, 60) + "..."
                        : cleanContent;
                }
                else
                {
                    item.Title = "General Post";
                }
            }

            var now = System.DateTime.UtcNow;
            item.CreatedAt = now;
            item.UpdatedAt = now;
            item.LikeCount = 0;
            item.ViewCount = 0;

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
