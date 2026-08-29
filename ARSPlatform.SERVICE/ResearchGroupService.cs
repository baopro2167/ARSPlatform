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
    public class ResearchGroupService : IResearchGroupService
    {
        private readonly IResearchGroupRepository _repository;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public ResearchGroupService(
            IResearchGroupRepository repository,
            IGroupMemberRepository groupMemberRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IMapper mapper)
        {
            _repository = repository;
            _groupMemberRepository = groupMemberRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ResearchGroupResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync(includes: new Expression<Func<ResearchGroup, object>>[]
            {
                x => x.Lecturer!,
                x => x.Topic!,
                x => x.GroupMembers
            });
            return _mapper.Map<IEnumerable<ResearchGroupResponse>>(items);
        }

        public async Task<IEnumerable<ResearchGroupResponse>> GetMyGroupsAsync(int currentUserId)
        {
            var items = await _repository.GetAllAsync(
                predicate: x => x.LecturerId == currentUserId || x.GroupMembers.Any(gm => gm.StudentId == currentUserId),
                includes: new Expression<Func<ResearchGroup, object>>[]
                {
                    x => x.Lecturer!,
                    x => x.Topic!,
                    x => x.GroupMembers
                });
            return _mapper.Map<IEnumerable<ResearchGroupResponse>>(items);
        }

        public async Task<PagedResult<ResearchGroupResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new Expression<Func<ResearchGroup, object>>[]
                {
                    x => x.Lecturer!,
                    x => x.Topic!,
                    x => x.GroupMembers
                });
            var dtos = _mapper.Map<List<ResearchGroupResponse>>(paged.Items);
            return new PagedResult<ResearchGroupResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ResearchGroupResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByLecturerIdPagedAsync(lecturerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ResearchGroupResponse>>(paged.Items);
            return new PagedResult<ResearchGroupResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ResearchGroupResponse>> GetByTopicIdAsync(int topicId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByTopicIdPagedAsync(topicId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ResearchGroupResponse>>(paged.Items);
            return new PagedResult<ResearchGroupResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ResearchGroupResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ResearchGroupResponse?> GetByIdAsync(int id)
        {
            var item = (await _repository.GetAllAsync(x => x.ResearchGroupId == id,
                x => x.Lecturer!,
                x => x.Topic!,
                x => x.GroupMembers)).FirstOrDefault();
            return item == null ? null : _mapper.Map<ResearchGroupResponse>(item);
        }

        public async Task<ResearchGroupResponse> CreateAsync(ResearchGroupCreateRequest request, int? lecturerId = null)
        {
            var item = _mapper.Map<ResearchGroup>(request);
            if (lecturerId.HasValue && !item.LecturerId.HasValue)
            {
                item.LecturerId = lecturerId.Value;
            }
            item.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            var created = await GetByIdAsync(item.ResearchGroupId);
            return created ?? _mapper.Map<ResearchGroupResponse>(item);
        }

        public async Task<ResearchGroupResponse?> UpdateAsync(int id, ResearchGroupUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<ResearchGroupResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<ResearchGroupInviteResponse> InviteStudentsAsync(int researchGroupId, ResearchGroupInviteRequest request, int currentUserId)
        {
            var group = (await _repository.GetAllAsync(x => x.ResearchGroupId == researchGroupId, x => x.GroupMembers)).FirstOrDefault();

            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {researchGroupId} not found.");
            }

            var response = new ResearchGroupInviteResponse
            {
                ResearchGroupId = researchGroupId
            };

            if (request.Emails == null || !request.Emails.Any())
            {
                return response;
            }

            var currentMemberStudentIds = group.GroupMembers
                .Where(gm => gm.StudentId.HasValue)
                .Select(gm => gm.StudentId!.Value)
                .ToHashSet();

            foreach (var rawEmail in request.Emails)
            {
                var email = rawEmail.Trim();
                if (string.IsNullOrWhiteSpace(email)) continue;

                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    response.NotFoundEmails.Add(email);
                    continue;
                }

                if (currentMemberStudentIds.Contains(user.UserId))
                {
                    response.AlreadyMemberEmails.Add(email);
                    continue;
                }

                var newMember = new GroupMember
                {
                    ResearchGroupId = researchGroupId,
                    StudentId = user.UserId,
                    ActivityStatus = "JOINED",
                    JoinedAt = DateTime.UtcNow
                };

                await _groupMemberRepository.AddAsync(newMember);
                currentMemberStudentIds.Add(user.UserId);
                response.SuccessEmails.Add(email);
                response.TotalInvited++;

                // Sinh Notification cho sinh viên
                try
                {
                    var notification = new Notification
                    {
                        UserId = user.UserId,
                        Message = $"[Nhóm nghiên cứu] Bạn đã được thêm vào nhóm nghiên cứu \"{group.Name}\"",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _notificationRepository.AddAsync(notification);
                }
                catch
                {
                    // Ignore notification errors
                }
            }

            await _groupMemberRepository.SaveChangesAsync();
            await _notificationRepository.SaveChangesAsync();

            return response;
        }
    }
}
