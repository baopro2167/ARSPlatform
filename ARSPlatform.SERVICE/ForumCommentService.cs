using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public class ForumCommentService : IForumCommentService
    {
        private readonly IForumCommentRepository _repository;
        private readonly IMapper _mapper;

        public ForumCommentService(IForumCommentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ForumCommentResponse>> GetAllAsync(int? postId = null)
        {
            Expression<Func<ForumComment, bool>>? predicate = postId.HasValue ? x => x.ForumPostId == postId.Value : null;
            var items = await _repository.GetAllAsync(predicate);
            return _mapper.Map<IEnumerable<ForumCommentResponse>>(items);
        }

        public async Task<PagedResult<ForumCommentResponse>> GetPagedAsync(PaginationParams paginationParams, int? postId = null)
        {
            Expression<Func<ForumComment, bool>>? predicate = postId.HasValue ? x => x.ForumPostId == postId.Value : null;
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate);
            var dtos = _mapper.Map<List<ForumCommentResponse>>(paged.Items);
            return new PagedResult<ForumCommentResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ForumCommentResponse>> GetByPostIdAsync(int postId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByPostIdPagedAsync(postId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ForumCommentResponse>>(paged.Items);
            return new PagedResult<ForumCommentResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ForumCommentResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByUserIdPagedAsync(userId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ForumCommentResponse>>(paged.Items);
            return new PagedResult<ForumCommentResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ForumCommentResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ForumCommentResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<ForumCommentResponse>(item);
        }

        public async Task<ForumCommentResponse> CreateAsync(ForumCommentCreateRequest request, int userId)
        {
            var item = _mapper.Map<ForumComment>(request);
            item.UserId = userId;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<ForumCommentResponse>(item);
        }

        public async Task<ForumCommentResponse?> UpdateAsync(int id, ForumCommentUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<ForumCommentResponse>(item);
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
