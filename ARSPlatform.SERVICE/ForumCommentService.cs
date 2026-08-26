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
    public class ForumCommentService : IForumCommentService
    {
        private readonly IForumCommentRepository _repository;
        private readonly IMapper _mapper;

        public ForumCommentService(IForumCommentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ForumCommentResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ForumCommentResponse>>(items);
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
