using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using ARSPlatform.REPOSITORIES;

namespace ARSPlatform.SERVICES
{
    public class ResearchGroupJoinRequestService : IResearchGroupJoinRequestService
    {
        private readonly IResearchGroupJoinRequestRepository _joinRequestRepository;
        private readonly IResearchGroupRepository _researchGroupRepository;
        private readonly IGroupMemberRepository _groupMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IMapper _mapper;
        private readonly ISignalRNotificationService? _realtimeService;

        public ResearchGroupJoinRequestService(
            IResearchGroupJoinRequestRepository joinRequestRepository,
            IResearchGroupRepository researchGroupRepository,
            IGroupMemberRepository groupMemberRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IDbContextFactory<AppDbContext> dbContextFactory,
            IMapper mapper,
            ISignalRNotificationService? realtimeService = null)
        {
            _joinRequestRepository = joinRequestRepository;
            _researchGroupRepository = researchGroupRepository;
            _groupMemberRepository = groupMemberRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _dbContextFactory = dbContextFactory;
            _mapper = mapper;
            _realtimeService = realtimeService;
        }

        public async Task<ResearchGroupJoinRequestResponse> CreateJoinRequestAsync(int groupId, int applicantUserId, string? note = null)
        {
            var group = await _researchGroupRepository.GetWithMembersAsync(groupId);

            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (!group.IsActive)
            {
                throw new InvalidOperationException("This research group is currently inactive.");
            }

            var isAlreadyMember = group.GroupMembers.Any(gm =>
                gm.StudentId == applicantUserId &&
                gm.ActivityStatus != "REJECTED" &&
                gm.ActivityStatus != "LEFT");

            if (isAlreadyMember)
            {
                throw new InvalidOperationException("You are already a member of this research group.");
            }

            var hasPendingRequest = await _joinRequestRepository.HasPendingRequestAsync(applicantUserId, groupId);

            if (hasPendingRequest)
            {
                throw new InvalidOperationException("You already have a pending join request for this research group.");
            }

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

            var applicant = await _userRepository.GetWithRoleByIdAsync(applicantUserId);

            var applicantName = applicant?.Profile?.FullName ?? applicant?.FullName ?? "Sinh viên";

            var joinRequest = new ResearchGroupJoinRequest
            {
                ResearchGroupId = groupId,
                ApplicantUserId = applicantUserId,
                Status = "PENDING",
                RejectionNote = note,
                CreatedAt = DateTime.UtcNow
            };

            await using var ctx = await _dbContextFactory.CreateDbContextAsync();
            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await ctx.Database.BeginTransactionAsync();
                try
                {
                    var localJoinReqRepo = new ResearchGroupJoinRequestRepository(ctx);
                    var localNotifRepo = new NotificationRepository(ctx);
                    
                    await localJoinReqRepo.AddAsync(joinRequest);
                    
                    if (group.LecturerId.HasValue)
                    {
                        var notifLecturer = new Notification
                        {
                            UserId = group.LecturerId.Value,
                            Message = $"[Nhóm nghiên cứu] Sinh viên {applicantName} đã gửi yêu cầu tham gia nhóm nghiên cứu \"{group.Name}\".",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await localNotifRepo.AddAsync(notifLecturer);
                    }
                    
                    await ctx.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            if (_realtimeService != null)
            {
                await _realtimeService.SendGroupJoinRequestUpdatedAsync(
                    groupId,
                    joinRequest.JoinRequestId,
                    joinRequest.Status,
                    applicantUserId,
                    applicantName);
            }

            var loaded = await _joinRequestRepository.GetWithDetailsAsync(joinRequest.JoinRequestId);

            return _mapper.Map<ResearchGroupJoinRequestResponse>(loaded ?? joinRequest);
        }

        public async Task<IEnumerable<ResearchGroupJoinRequestResponse>> GetJoinRequestsForLecturerAsync(int groupId, int currentUserId, string? status = null)
        {
            var group = await _researchGroupRepository.GetByIdAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (group.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view join requests for this research group.");
            }

            var list = await _joinRequestRepository.GetListWithDetailsAsync(groupId, status);
            return _mapper.Map<IEnumerable<ResearchGroupJoinRequestResponse>>(list);
        }

        public async Task<ResearchGroupJoinRequestResponse?> GetJoinRequestByIdAsync(int groupId, int joinRequestId, int currentUserId)
        {
            var group = await _researchGroupRepository.GetByIdAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (group.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this join request.");
            }

            var item = await _joinRequestRepository.GetWithDetailsAsync(joinRequestId, groupId);

            return item == null ? null : _mapper.Map<ResearchGroupJoinRequestResponse>(item);
        }

        public async Task<ResearchGroupJoinRequestResponse> AcceptJoinRequestAsync(int groupId, int joinRequestId, int currentUserId)
        {
            var group = await _researchGroupRepository.GetWithMembersAsync(groupId);

            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (group.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to accept join requests for this research group.");
            }

            var joinRequest = await _joinRequestRepository.GetWithDetailsAsync(joinRequestId, groupId);

            if (joinRequest == null)
            {
                throw new KeyNotFoundException($"Join request with ID {joinRequestId} not found for this group.");
            }

            if (joinRequest.Status != "PENDING")
            {
                throw new InvalidOperationException($"Join request is not pending (current status: {joinRequest.Status}).");
            }

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

            await using var ctx = await _dbContextFactory.CreateDbContextAsync();
            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await ctx.Database.BeginTransactionAsync();
                try
                {
                    var localJoinReqRepo = new ResearchGroupJoinRequestRepository(ctx);
                    var localGroupMemberRepo = new GroupMemberRepository(ctx);
                    var localNotifRepo = new NotificationRepository(ctx);

                    var localJoinRequest = await localJoinReqRepo.GetByIdAsync(joinRequestId);
                    if (localJoinRequest != null)
                    {
                        localJoinRequest.Status = "ACCEPTED";
                        localJoinRequest.DecidedByUserId = currentUserId;
                        localJoinRequest.DecidedAt = DateTime.UtcNow;
                        localJoinRequest.UpdatedAt = DateTime.UtcNow;
                        localJoinReqRepo.Update(localJoinRequest);
                    }

                    var localGroup = await ctx.ResearchGroups.Include(g => g.GroupMembers).FirstOrDefaultAsync(g => g.ResearchGroupId == groupId);
                    var existingMember = localGroup?.GroupMembers.FirstOrDefault(gm => gm.StudentId == joinRequest.ApplicantUserId);

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
                        await localGroupMemberRepo.AddAsync(newMember);
                    }

                    var notifApplicant = new Notification
                    {
                        UserId = joinRequest.ApplicantUserId,
                        Message = $"[Nhóm nghiên cứu] Yêu cầu tham gia nhóm nghiên cứu \"{group.Name}\" của bạn đã được Giảng viên chấp thuận.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await localNotifRepo.AddAsync(notifApplicant);

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
                            await localNotifRepo.AddAsync(notifMember);
                        }
                    }

                    await ctx.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            if (_realtimeService != null)
            {
                await _realtimeService.SendGroupJoinRequestUpdatedAsync(
                    groupId,
                    joinRequestId,
                    "ACCEPTED",
                    joinRequest.ApplicantUserId,
                    applicantName);
            }

            var updated = await _joinRequestRepository.GetWithDetailsAsync(joinRequestId);

            return _mapper.Map<ResearchGroupJoinRequestResponse>(updated ?? joinRequest);
        }

        public async Task<ResearchGroupJoinRequestResponse> RejectJoinRequestAsync(int groupId, int joinRequestId, string? rejectionNote, int currentUserId)
        {
            var group = await _researchGroupRepository.GetByIdAsync(groupId);
            if (group == null)
            {
                throw new KeyNotFoundException($"Research group with ID {groupId} not found.");
            }

            if (group.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to reject join requests for this research group.");
            }

            var joinRequest = await _joinRequestRepository.GetWithDetailsAsync(joinRequestId, groupId);

            if (joinRequest == null)
            {
                throw new KeyNotFoundException($"Join request with ID {joinRequestId} not found for this group.");
            }

            if (joinRequest.Status != "PENDING")
            {
                throw new InvalidOperationException($"Join request is not pending (current status: {joinRequest.Status}).");
            }

            await using var ctx = await _dbContextFactory.CreateDbContextAsync();
            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await ctx.Database.BeginTransactionAsync();
                try
                {
                    var localJoinReqRepo = new ResearchGroupJoinRequestRepository(ctx);
                    var localNotifRepo = new NotificationRepository(ctx);

                    var localJoinRequest = await localJoinReqRepo.GetByIdAsync(joinRequestId);
                    if (localJoinRequest != null)
                    {
                        localJoinRequest.Status = "REJECTED";
                        localJoinRequest.RejectionNote = rejectionNote;
                        localJoinRequest.DecidedByUserId = currentUserId;
                        localJoinRequest.DecidedAt = DateTime.UtcNow;
                        localJoinRequest.UpdatedAt = DateTime.UtcNow;
                        localJoinReqRepo.Update(localJoinRequest);
                    }

                    var reasonText = !string.IsNullOrWhiteSpace(rejectionNote) ? $" Lý do: {rejectionNote}" : "";
                    var notifApplicant = new Notification
                    {
                        UserId = joinRequest.ApplicantUserId,
                        Message = $"[Nhóm nghiên cứu] Yêu cầu tham gia nhóm nghiên cứu \"{group.Name}\" của bạn đã bị từ chối.{reasonText}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await localNotifRepo.AddAsync(notifApplicant);

                    await ctx.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            if (_realtimeService != null)
            {
                var applicantName = joinRequest.ApplicantUser?.Profile?.FullName ?? joinRequest.ApplicantUser?.FullName ?? "Sinh viên";
                await _realtimeService.SendGroupJoinRequestUpdatedAsync(
                    groupId,
                    joinRequestId,
                    "REJECTED",
                    joinRequest.ApplicantUserId,
                    applicantName);
            }

            var updated = await _joinRequestRepository.GetWithDetailsAsync(joinRequestId);

            return _mapper.Map<ResearchGroupJoinRequestResponse>(updated ?? joinRequest);
        }
    }
}
