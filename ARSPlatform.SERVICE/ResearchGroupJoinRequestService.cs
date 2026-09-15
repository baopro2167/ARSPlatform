using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class ResearchGroupJoinRequestService : IResearchGroupJoinRequestService
    {
        private readonly IResearchGroupJoinRequestRepository _joinRequestRepository;
        private readonly IResearchGroupRepository _researchGroupRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public ResearchGroupJoinRequestService(
            IResearchGroupJoinRequestRepository joinRequestRepository,
            IResearchGroupRepository researchGroupRepository,
            IGroupMemberRepository groupMemberRepository,
            IUserRepository userRepository,
            AppDbContext dbContext,
            IMapper mapper)
        {
            _joinRequestRepository = joinRequestRepository;
            _researchGroupRepository = researchGroupRepository;
            _groupMemberRepository = groupMemberRepository;
            _userRepository = userRepository;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<ResearchGroupJoinRequestResponse> CreateJoinRequestAsync(int groupId, int applicantUserId, string? note = null)
        {
            var group = await _dbContext.ResearchGroups
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.ResearchGroupId == groupId);

            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (!group.IsActive)
            {
                throw new InvalidOperationException("This research group is currently inactive.");
            }

            // Check if student is already an active member of this group
            var isAlreadyMember = group.GroupMembers.Any(gm =>
                gm.StudentId == applicantUserId &&
                gm.ActivityStatus != "REJECTED" &&
                gm.ActivityStatus != "LEFT");

            if (isAlreadyMember)
            {
                throw new InvalidOperationException("You are already a member of this research group.");
            }

            // Check if applicant already has a pending join request for this group
            var hasPendingRequest = await _dbContext.ResearchGroupJoinRequests
                .AnyAsync(r => r.ResearchGroupId == groupId && r.ApplicantUserId == applicantUserId && r.Status == "PENDING");

            if (hasPendingRequest)
            {
                throw new InvalidOperationException("You already have a pending join request for this research group.");
            }

            // Check capacity of the group
            if (group.MaxMembers.HasValue)
            {
                var activeMemberCount = group.GroupMembers.Count(gm =>
                    gm.ActivityStatus != "REJECTED" &&
                    gm.ActivityStatus != "LEFT");

                if (activeMemberCount >= group.MaxMembers.Value)
                {
                    throw new InvalidOperationException($"Research group has reached maximum member capacity ({group.MaxMembers.Value} members).");
                }
            }

            var applicant = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserId == applicantUserId);

            var applicantName = applicant?.Profile?.FullName ?? applicant?.FullName ?? "Sinh viên";

            var joinRequest = new ResearchGroupJoinRequest
            {
                ResearchGroupId = groupId,
                ApplicantUserId = applicantUserId,
                Status = "PENDING",
                RejectionNote = note,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.ResearchGroupJoinRequests.AddAsync(joinRequest);

            // Notification: RESEARCH_GROUP_JOIN_REQUESTED -> Send to Owner Lecturer
            if (group.LecturerId.HasValue)
            {
                var notifLecturer = new Notification
                {
                    UserId = group.LecturerId.Value,
                    Message = $"[Nhóm nghiên cứu] Sinh viên {applicantName} đã gửi yêu cầu tham gia nhóm nghiên cứu \"{group.Name}\".",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _dbContext.Notifications.AddAsync(notifLecturer);
            }

            await _dbContext.SaveChangesAsync();

            var loaded = await _dbContext.ResearchGroupJoinRequests
                .Include(r => r.ResearchGroup)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.Profile)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.ProfessionalProfile).ThenInclude(pp => pp.SubField)
                .FirstOrDefaultAsync(r => r.JoinRequestId == joinRequest.JoinRequestId);

            return _mapper.Map<ResearchGroupJoinRequestResponse>(loaded ?? joinRequest);
        }

        public async Task<IEnumerable<ResearchGroupJoinRequestResponse>> GetJoinRequestsForLecturerAsync(int groupId, int currentUserId, string? status = null)
        {
            var group = await _dbContext.ResearchGroups.FirstOrDefaultAsync(g => g.ResearchGroupId == groupId);
            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (group.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view join requests for this research group.");
            }

            var query = _dbContext.ResearchGroupJoinRequests
                .Include(r => r.ResearchGroup)
                .Include(r => r.DecidedByUser)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.Profile)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.ProfessionalProfile).ThenInclude(pp => pp.SubField)
                .Where(r => r.ResearchGroupId == groupId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return _mapper.Map<IEnumerable<ResearchGroupJoinRequestResponse>>(list);
        }

        public async Task<ResearchGroupJoinRequestResponse?> GetJoinRequestByIdAsync(int groupId, int joinRequestId, int currentUserId)
        {
            var group = await _dbContext.ResearchGroups.FirstOrDefaultAsync(g => g.ResearchGroupId == groupId);
            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (group.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this join request.");
            }

            var item = await _dbContext.ResearchGroupJoinRequests
                .Include(r => r.ResearchGroup)
                .Include(r => r.DecidedByUser)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.Profile)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.ProfessionalProfile).ThenInclude(pp => pp.SubField)
                .FirstOrDefaultAsync(r => r.JoinRequestId == joinRequestId && r.ResearchGroupId == groupId);

            return item == null ? null : _mapper.Map<ResearchGroupJoinRequestResponse>(item);
        }

        public async Task<ResearchGroupJoinRequestResponse> AcceptJoinRequestAsync(int groupId, int joinRequestId, int currentUserId)
        {
            var group = await _dbContext.ResearchGroups
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.ResearchGroupId == groupId);

            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (group.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to accept join requests for this research group.");
            }

            var joinRequest = await _dbContext.ResearchGroupJoinRequests
                .Include(r => r.ApplicantUser).ThenInclude(u => u.Profile)
                .FirstOrDefaultAsync(r => r.JoinRequestId == joinRequestId && r.ResearchGroupId == groupId);

            if (joinRequest == null)
            {
                throw new KeyNotFoundException($"Join request with ID {joinRequestId} not found for this group.");
            }

            if (joinRequest.Status != "PENDING")
            {
                throw new InvalidOperationException($"Join request is not pending (current status: {joinRequest.Status}).");
            }

            // Check group capacity
            var activeMembers = group.GroupMembers
                .Where(gm => gm.ActivityStatus != "REJECTED" && gm.ActivityStatus != "LEFT")
                .ToList();

            if (group.MaxMembers.HasValue && activeMembers.Count >= group.MaxMembers.Value)
            {
                throw new InvalidOperationException($"Research group has reached maximum member capacity ({group.MaxMembers.Value} members).");
            }

            var applicantName = joinRequest.ApplicantUser?.Profile?.FullName
                ?? joinRequest.ApplicantUser?.FullName
                ?? "Sinh viên";

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // 1. Update Join Request Status
                    joinRequest.Status = "ACCEPTED";
                    joinRequest.DecidedByUserId = currentUserId;
                    joinRequest.DecidedAt = DateTime.UtcNow;
                    joinRequest.UpdatedAt = DateTime.UtcNow;

                    // 2. Insert or Update GroupMember
                    var existingMember = group.GroupMembers
                        .FirstOrDefault(gm => gm.StudentId == joinRequest.ApplicantUserId);

                    if (existingMember != null)
                    {
                        existingMember.ActivityStatus = "JOINED";
                        existingMember.JoinedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var newMember = new GroupMember
                        {
                            ResearchGroupId = groupId,
                            StudentId = joinRequest.ApplicantUserId,
                            ActivityStatus = "JOINED",
                            JoinedAt = DateTime.UtcNow,
                            LeaderId = false
                        };
                        await _dbContext.GroupMembers.AddAsync(newMember);
                    }

                    // 3. Notification Event: RESEARCH_GROUP_JOIN_REQUEST_ACCEPTED -> To Applicant Student
                    var notifApplicant = new Notification
                    {
                        UserId = joinRequest.ApplicantUserId,
                        Message = $"[Nhóm nghiên cứu] Yêu cầu tham gia nhóm nghiên cứu \"{group.Name}\" của bạn đã được Giảng viên chấp thuận.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _dbContext.Notifications.AddAsync(notifApplicant);

                    // 4. Notification Event: RESEARCH_GROUP_MEMBER_ACCEPTED -> Fan-out to all group members
                    foreach (var member in activeMembers)
                    {
                        if (member.StudentId.HasValue && member.StudentId.Value != joinRequest.ApplicantUserId)
                        {
                            var notifMember = new Notification
                            {
                                UserId = member.StudentId.Value,
                                Message = $"[Nhóm nghiên cứu] Sinh viên {applicantName} đã gia nhập nhóm nghiên cứu \"{group.Name}\".",
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _dbContext.Notifications.AddAsync(notifMember);
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            var updated = await _dbContext.ResearchGroupJoinRequests
                .Include(r => r.ResearchGroup)
                .Include(r => r.DecidedByUser)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.Profile)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.ProfessionalProfile).ThenInclude(pp => pp.SubField)
                .FirstOrDefaultAsync(r => r.JoinRequestId == joinRequestId);

            return _mapper.Map<ResearchGroupJoinRequestResponse>(updated ?? joinRequest);
        }

        public async Task<ResearchGroupJoinRequestResponse> RejectJoinRequestAsync(int groupId, int joinRequestId, string? rejectionNote, int currentUserId)
        {
            var group = await _dbContext.ResearchGroups.FirstOrDefaultAsync(g => g.ResearchGroupId == groupId);
            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (group.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to reject join requests for this research group.");
            }

            var joinRequest = await _dbContext.ResearchGroupJoinRequests
                .Include(r => r.ApplicantUser).ThenInclude(u => u.Profile)
                .FirstOrDefaultAsync(r => r.JoinRequestId == joinRequestId && r.ResearchGroupId == groupId);

            if (joinRequest == null)
            {
                throw new KeyNotFoundException($"Join request with ID {joinRequestId} not found for this group.");
            }

            if (joinRequest.Status != "PENDING")
            {
                throw new InvalidOperationException($"Join request is not pending (current status: {joinRequest.Status}).");
            }

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // 1. Update status
                    joinRequest.Status = "REJECTED";
                    joinRequest.RejectionNote = rejectionNote;
                    joinRequest.DecidedByUserId = currentUserId;
                    joinRequest.DecidedAt = DateTime.UtcNow;
                    joinRequest.UpdatedAt = DateTime.UtcNow;

                    // 2. Notification Event: RESEARCH_GROUP_JOIN_REQUEST_REJECTED -> To Applicant Student
                    var reasonText = !string.IsNullOrWhiteSpace(rejectionNote) ? $" Lý do: {rejectionNote}" : "";
                    var notifApplicant = new Notification
                    {
                        UserId = joinRequest.ApplicantUserId,
                        Message = $"[Nhóm nghiên cứu] Yêu cầu tham gia nhóm nghiên cứu \"{group.Name}\" của bạn đã bị từ chối.{reasonText}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _dbContext.Notifications.AddAsync(notifApplicant);

                    await _dbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            var updated = await _dbContext.ResearchGroupJoinRequests
                .Include(r => r.ResearchGroup)
                .Include(r => r.DecidedByUser)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.Profile)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.ProfessionalProfile).ThenInclude(pp => pp.SubField)
                .FirstOrDefaultAsync(r => r.JoinRequestId == joinRequestId);

            return _mapper.Map<ResearchGroupJoinRequestResponse>(updated ?? joinRequest);
        }
    }
}
