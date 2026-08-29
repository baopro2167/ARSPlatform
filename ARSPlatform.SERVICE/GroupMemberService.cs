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
    public class GroupMemberService : IGroupMemberService
    {
        private readonly IGroupMemberRepository _repository;
        private readonly IMapper _mapper;

        public GroupMemberService(IGroupMemberRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<GroupMemberResponse>> GetAllAsync(int? groupId = null)
        {
            Expression<Func<GroupMember, bool>>? predicate = groupId.HasValue ? x => x.ResearchGroupId == groupId.Value : null;
            var items = await _repository.GetAllAsync(predicate);
            return _mapper.Map<IEnumerable<GroupMemberResponse>>(items);
        }

        public async Task<PagedResult<GroupMemberResponse>> GetPagedAsync(PaginationParams paginationParams, int? groupId = null)
        {
            Expression<Func<GroupMember, bool>>? predicate = groupId.HasValue ? x => x.ResearchGroupId == groupId.Value : null;
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate);
            var dtos = _mapper.Map<List<GroupMemberResponse>>(paged.Items);
            return new PagedResult<GroupMemberResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<GroupMemberResponse>> GetByGroupIdAsync(int groupId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByGroupIdPagedAsync(groupId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<GroupMemberResponse>>(paged.Items);
            return new PagedResult<GroupMemberResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<GroupMemberResponse>> GetByStudentIdAsync(int studentId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByStudentIdPagedAsync(studentId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<GroupMemberResponse>>(paged.Items);
            return new PagedResult<GroupMemberResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<GroupMemberResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<GroupMemberResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<GroupMemberResponse>(item);
        }

        public async Task<GroupMemberResponse> CreateAsync(GroupMemberCreateRequest request)
        {
            var item = _mapper.Map<GroupMember>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<GroupMemberResponse>(item);
        }

        public async Task<GroupMemberResponse?> UpdateAsync(int id, GroupMemberUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<GroupMemberResponse>(item);
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
