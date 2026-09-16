using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.SERVICES
{
    public class GroupMemberService : IGroupMemberService
    {
        private readonly IGroupMemberRepository _repository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _dbContext;

        public GroupMemberService(IGroupMemberRepository repository, IMapper mapper, AppDbContext dbContext)
        {
            _repository = repository;
            _mapper = mapper;
            _dbContext = dbContext;
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

        public async Task<PagedResult<GroupMemberResponse>> GetByActivityStatusAsync(
            string status,
            PaginationParams paginationParams,
            int? groupId = null)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Status is required (e.g. PENDING or REJECTED).", nameof(status));
            }

            var normalizedStatus = status.Trim().ToUpperInvariant();

            // Repository không có sẵn method này → query trực tiếp qua DbContext
            // để vẫn include được Student + ResearchGroup.
            var query = _dbContext.GroupMembers
                .Include(x => x.Student!)
                .Include(x => x.ResearchGroup!)
                .AsQueryable();

            query = query.Where(x =>
                x.ActivityStatus != null &&
                x.ActivityStatus.ToUpper() == normalizedStatus);

            if (groupId.HasValue)
            {
                query = query.Where(x => x.ResearchGroupId == groupId.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.JoinedAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.GroupMemberId)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<GroupMemberResponse>>(items);
            return new PagedResult<GroupMemberResponse>(
                dtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
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

            // Gửi thông báo cho thành viên mới được thêm
            if (item.StudentId.HasValue && item.ResearchGroupId.HasValue)
            {
                try
                {
                    var group = await _dbContext.ResearchGroups.AsNoTracking().FirstOrDefaultAsync(g => g.ResearchGroupId == item.ResearchGroupId.Value);
                    var groupName = group?.Name ?? "Nhóm nghiên cứu";
                    var notif = new Notification
                    {
                        UserId = item.StudentId.Value,
                        Message = $"[Nhóm nghiên cứu] Bạn đã được thêm vào nhóm nghiên cứu \"{groupName}\".",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _dbContext.Notifications.AddAsync(notif);
                    await _dbContext.SaveChangesAsync();
                }
                catch
                {
                    // Ignore notification error
                }
            }

            var created = (await _repository.GetAllAsync(x => x.GroupMemberId == item.GroupMemberId, includes: x => x.Student!)).FirstOrDefault();
            return _mapper.Map<GroupMemberResponse>(created ?? item);
        }

        public async Task<GroupMemberResponse?> UpdateAsync(int id, GroupMemberUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            var oldStatus = item.ActivityStatus;
            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            // Gửi thông báo khi thành viên chấp nhận tham gia hoặc trạng thái thay đổi
            if (item.ResearchGroupId.HasValue && !string.IsNullOrWhiteSpace(request.ActivityStatus) && oldStatus != item.ActivityStatus)
            {
                try
                {
                    var group = await _dbContext.ResearchGroups.AsNoTracking().FirstOrDefaultAsync(g => g.ResearchGroupId == item.ResearchGroupId.Value);
                    var student = item.StudentId.HasValue ? await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == item.StudentId.Value) : null;
                    var groupName = group?.Name ?? "Nhóm nghiên cứu";
                    var studentName = student?.FullName ?? "Thành viên";

                    var normalizedStatus = request.ActivityStatus.Trim().ToLowerInvariant();
                    if (normalizedStatus is "accepted" or "joined" or "active")
                    {
                        if (group?.LecturerId.HasValue == true && group.LecturerId.Value != item.StudentId)
                        {
                            var notif = new Notification
                            {
                                UserId = group.LecturerId.Value,
                                Message = $"[Nhóm nghiên cứu] {studentName} đã đồng ý tham gia nhóm nghiên cứu \"{groupName}\".",
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _dbContext.Notifications.AddAsync(notif);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }
                catch
                {
                    // Ignore notification error
                }
            }

            var updated = (await _repository.GetAllAsync(x => x.GroupMemberId == id, includes: x => x.Student!)).FirstOrDefault();
            return _mapper.Map<GroupMemberResponse>(updated ?? item);
        }

        /// <summary>
        /// Lecturer duyệt hoặc từ chối một sinh viên trong nhóm.
        /// Endpoint chuyên biệt: chỉ thay đổi ActivityStatus và RequestNote,
        /// tự động gửi notification cho Student tương ứng.
        /// </summary>
        public async Task<GroupMemberResponse> UpdateApprovalAsync(int groupMemberId, GroupMemberApprovalRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Request body is required.");
            }

            var item = (await _repository.GetAllAsync(
                x => x.GroupMemberId == groupMemberId,
                includes: x => x.Student!)).FirstOrDefault();

            if (item == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy thành viên nhóm với ID {groupMemberId}.");
            }

            // Chỉ chấp nhận các status hợp lệ
            var allowedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "PENDING", "JOINED", "ACCEPTED", "ACTIVE", "REJECTED", "LEFT", "REMOVED"
            };

            var newStatusRaw = request.ActivityStatus?.Trim();
            if (string.IsNullOrWhiteSpace(newStatusRaw))
            {
                throw new ArgumentException("ActivityStatus is required (e.g. JOINED, REJECTED, PENDING).", nameof(request.ActivityStatus));
            }

            if (!allowedStatuses.Contains(newStatusRaw))
            {
                throw new ArgumentException(
                    $"ActivityStatus '{newStatusRaw}' không hợp lệ. Chỉ chấp nhận: PENDING, JOINED, ACCEPTED, ACTIVE, REJECTED, LEFT, REMOVED.",
                    nameof(request.ActivityStatus));
            }

            // Chuẩn hoá: lưu trữ dạng chữ IN HOA để truy vấn GetByActivityStatusAsync đồng bộ
            var normalizedStatus = newStatusRaw.ToUpperInvariant();

            // Nếu Lecturer vừa duyệt thì set JoinedAt = UtcNow (nếu trước đó chưa có)
            var wasPending = string.Equals(item.ActivityStatus, "PENDING", StringComparison.OrdinalIgnoreCase);
            var wasRejected = string.Equals(item.ActivityStatus, "REJECTED", StringComparison.OrdinalIgnoreCase);

            item.ActivityStatus = normalizedStatus;
            item.RequestNote = string.IsNullOrWhiteSpace(request.RequestNote)
                ? null
                : request.RequestNote.Trim();

            if ((normalizedStatus == "JOINED" || normalizedStatus == "ACCEPTED" || normalizedStatus == "ACTIVE")
                && !item.JoinedAt.HasValue)
            {
                item.JoinedAt = DateTime.UtcNow;
            }

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            // ─────────────────────────────────────────────
            // Gửi notification cho Student
            // ─────────────────────────────────────────────
            if (item.StudentId.HasValue && item.ResearchGroupId.HasValue)
            {
                try
                {
                    var group = await _dbContext.ResearchGroups
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.ResearchGroupId == item.ResearchGroupId.Value);
                    var groupName = group?.Name ?? "Nhóm nghiên cứu";

                    var noteSuffix = string.IsNullOrWhiteSpace(item.RequestNote)
                        ? string.Empty
                        : $" Lý do: {item.RequestNote}";

                    string message;

                    if (normalizedStatus is "JOINED" or "ACCEPTED" or "ACTIVE")
                    {
                        message = $"[Nhóm nghiên cứu] Yêu cầu gia nhập nhóm nghiên cứu \"{groupName}\" của bạn đã được Giảng viên chấp thuận.{noteSuffix}";
                    }
                    else if (normalizedStatus == "REJECTED")
                    {
                        message = $"[Nhóm nghiên cứu] Yêu cầu gia nhập nhóm nghiên cứu \"{groupName}\" của bạn đã bị từ chối.{noteSuffix}";
                    }
                    else if (normalizedStatus == "LEFT" || normalizedStatus == "REMOVED")
                    {
                        message = $"[Nhóm nghiên cứu] Bạn đã được cập nhật trạng thái rời khỏi nhóm nghiên cứu \"{groupName}\".{noteSuffix}";
                    }
                    else
                    {
                        message = $"[Nhóm nghiên cứu] Trạng thái yêu cầu tham gia nhóm \"{groupName}\" của bạn đã được cập nhật thành \"{normalizedStatus}\".{noteSuffix}";
                    }

                    var notif = new Notification
                    {
                        UserId = item.StudentId.Value,
                        Message = message,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _dbContext.Notifications.AddAsync(notif);
                    await _dbContext.SaveChangesAsync();
                }
                catch
                {
                    // Không để lỗi notification chặn response duyệt
                }
            }

            // Trả về bản ghi đã refresh kèm Student navigation
            var refreshed = (await _repository.GetAllAsync(
                x => x.GroupMemberId == groupMemberId,
                includes: x => x.Student!)).FirstOrDefault();

            return _mapper.Map<GroupMemberResponse>(refreshed ?? item);
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

            // Gửi thông báo cho thành viên được chỉ định làm Trưởng nhóm
            if (item.StudentId.HasValue && item.ResearchGroupId.HasValue)
            {
                try
                {
                    var group = await _dbContext.ResearchGroups.AsNoTracking().FirstOrDefaultAsync(g => g.ResearchGroupId == item.ResearchGroupId.Value);
                    var groupName = group?.Name ?? "Nhóm nghiên cứu";
                    var notif = new Notification
                    {
                        UserId = item.StudentId.Value,
                        Message = $"[Nhóm nghiên cứu] Bạn đã được chỉ định làm Trưởng nhóm (Leader) của nhóm \"{groupName}\".",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _dbContext.Notifications.AddAsync(notif);
                    await _dbContext.SaveChangesAsync();
                }
                catch
                {
                    // Ignore notification error
                }
            }

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
