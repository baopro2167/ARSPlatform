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
            var items = await _repository.GetAllAsync(predicate, includes: x => x.Student!);
            return _mapper.Map<IEnumerable<GroupMemberResponse>>(items);
        }

        public async Task<PagedResult<GroupMemberResponse>> GetPagedAsync(PaginationParams paginationParams, int? groupId = null)
        {
            Expression<Func<GroupMember, bool>>? predicate = groupId.HasValue ? x => x.ResearchGroupId == groupId.Value : null;
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate,
                includes: new Expression<Func<GroupMember, object>>[]
                {
                    x => x.Student!
                });
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
            var item = (await _repository.GetAllAsync(x => x.GroupMemberId == id, includes: x => x.Student!)).FirstOrDefault();
            return item == null ? null : _mapper.Map<GroupMemberResponse>(item);
        }

        public async Task<GroupMemberResponse> CreateAsync(GroupMemberCreateRequest request)
        {
            var item = _mapper.Map<GroupMember>(request);
            if (string.IsNullOrWhiteSpace(item.ActivityStatus))
            {
                item.ActivityStatus = "Joined";
            }
            item.JoinedAt ??= DateTime.UtcNow;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = (await _repository.GetAllAsync(x => x.GroupMemberId == item.GroupMemberId, includes: x => x.Student!)).FirstOrDefault();
            return _mapper.Map<GroupMemberResponse>(created ?? item);
        }

        public async Task<GroupMemberResponse?> UpdateAsync(int id, GroupMemberUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = (await _repository.GetAllAsync(x => x.GroupMemberId == id, includes: x => x.Student!)).FirstOrDefault();
            return _mapper.Map<GroupMemberResponse>(updated ?? item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<GroupMemberResponse> SetLeaderAsync(int groupMemberId, int? userId = null)
        {
            var item = (await _repository.GetAllAsync(x => x.GroupMemberId == groupMemberId, includes: x => x.Student!)).FirstOrDefault();
            if (item == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thành viên trong nhóm nghiên cứu.");
            }

            if (userId.HasValue && item.StudentId != userId.Value)
            {
                throw new ArgumentException("Mã người dùng (userId) không khớp với thành viên nhóm này.");
            }

            // Check if active status is Joined or Active
            var status = item.ActivityStatus?.Trim()?.ToLower();
            if (status != "joined" && status != "active")
            {
                throw new InvalidOperationException("Thành viên chưa được duyệt vào nhóm (ActiveStatus phải là 'Joined' hoặc 'Active') nên không thể gán làm Trưởng nhóm.");
            }

            // Check if another member is already a leader in this research group
            if (item.ResearchGroupId.HasValue)
            {
                var existingLeader = (await _repository.GetAllAsync(x =>
                    x.ResearchGroupId == item.ResearchGroupId.Value &&
                    x.GroupMemberId != item.GroupMemberId &&
                    x.LeaderId == true,
                    includes: x => x.Student!)).FirstOrDefault();

                if (existingLeader != null)
                {
                    var leaderName = existingLeader.Student?.FullName ?? $"ID {existingLeader.StudentId}";
                    throw new InvalidOperationException($"Nhóm nghiên cứu này đã có Trưởng nhóm (Leader) là {leaderName}.");
                }
            }

            item.LeaderId = true;
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = (await _repository.GetAllAsync(x => x.GroupMemberId == groupMemberId, includes: x => x.Student!)).FirstOrDefault();
            return _mapper.Map<GroupMemberResponse>(updated ?? item);
        }

        public async Task<GroupMemberResponse> RemoveLeaderAsync(int groupMemberId)
        {
            var item = (await _repository.GetAllAsync(x => x.GroupMemberId == groupMemberId, includes: x => x.Student!)).FirstOrDefault();
            if (item == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thành viên trong nhóm nghiên cứu.");
            }

            item.LeaderId = false;
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = (await _repository.GetAllAsync(x => x.GroupMemberId == groupMemberId, includes: x => x.Student!)).FirstOrDefault();
            return _mapper.Map<GroupMemberResponse>(updated ?? item);
        }
    }
}
