using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.SERVICES
{
    public class MedalService : IMedalService
    {
        private readonly IMedalRepository _medalRepo;
        private readonly IUserMedalRepository _userMedalRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;

        public MedalService(
            IMedalRepository medalRepo,
            IUserMedalRepository userMedalRepo,
            INotificationRepository notificationRepo,
            AppDbContext context,
            IMapper mapper,
            IAuditLogService auditLogService)
        {
            _medalRepo = medalRepo;
            _userMedalRepo = userMedalRepo;
            _notificationRepo = notificationRepo;
            _context = context;
            _mapper = mapper;
            _auditLogService = auditLogService;
        }

        #region Admin Methods

        public async Task<IEnumerable<MedalResponse>> GetAllAsync(string? role = null, string? tier = null, bool? isActive = null, string? search = null)
        {
            var medals = await _medalRepo.GetAllWithFiltersAsync(role, tier, isActive, search);
            return _mapper.Map<IEnumerable<MedalResponse>>(medals);
        }

        public async Task<MedalResponse?> GetByIdAsync(string id)
        {
            var medal = await _medalRepo.GetByIdAsync(id);
            return medal == null ? null : _mapper.Map<MedalResponse>(medal);
        }

        public async Task<MedalResponse> CreateAsync(MedalCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new ArgumentException("Title is required.");
            }

            var id = string.IsNullOrWhiteSpace(request.Id)
                ? "medal-" + Guid.NewGuid().ToString("N").Substring(0, 8)
                : request.Id.Trim();

            var code = string.IsNullOrWhiteSpace(request.Code)
                ? request.Title.Trim().ToUpper().Replace(" ", "_") + "_" + (request.Tier ?? "BRONZE").Trim().ToUpper()
                : request.Code.Trim().ToUpper();

            var codeExists = await _medalRepo.ExistsByCodeAsync(code);
            if (codeExists)
            {
                throw new InvalidOperationException($"A medal with code '{code}' already exists.");
            }

            var medal = _mapper.Map<Medal>(request);
            medal.Id = id;
            medal.Code = code;
            if (string.IsNullOrWhiteSpace(medal.ImageUrl)) medal.ImageUrl = "/images/default-medal.png";
            if (string.IsNullOrWhiteSpace(medal.CriteriaMetric)) medal.CriteriaMetric = "MANUAL";
            if (!string.IsNullOrWhiteSpace(request.MetricCode) && Enum.TryParse<MedalMetricCode>(request.MetricCode, true, out var pMetric))
            {
                medal.MetricCode = pMetric;
            }
            if (request.Rules != null)
            {
                medal.Rules = request.Rules;
            }
            if (request.ApplicableRoles != null && request.ApplicableRoles.Any())
            {
                medal.ApplicableRoles = JsonSerializer.Serialize(request.ApplicableRoles);
            }
            else if (request.Roles != null && request.Roles.Any())
            {
                medal.ApplicableRoles = JsonSerializer.Serialize(request.Roles);
            }
            medal.CreatedAt = DateTime.UtcNow;
            medal.UpdatedAt = DateTime.UtcNow;

            await _medalRepo.AddAsync(medal);
            await _medalRepo.SaveChangesAsync();

            var created = await _medalRepo.GetByIdAsync(medal.Id);
            return _mapper.Map<MedalResponse>(created);
        }

        public async Task<MedalResponse?> UpdateAsync(string id, MedalUpdateRequest request)
        {
            var medal = await _medalRepo.GetByIdAsync(id);
            if (medal == null) return null;

            if (!string.IsNullOrWhiteSpace(request.Title))
                medal.Title = request.Title.Trim();

            if (!string.IsNullOrWhiteSpace(request.TitleVi))
                medal.TitleVi = request.TitleVi.Trim();

            if (request.Description != null)
                medal.Description = request.Description.Trim();

            if (request.DescriptionVi != null)
                medal.DescriptionVi = request.DescriptionVi.Trim();

            if (request.Roles != null && request.Roles.Any())
                medal.Roles = JsonSerializer.Serialize(request.Roles);

            if (request.ApplicableRoles != null && request.ApplicableRoles.Any())
                medal.ApplicableRoles = JsonSerializer.Serialize(request.ApplicableRoles);

            if (!string.IsNullOrWhiteSpace(request.MetricCode) && Enum.TryParse<MedalMetricCode>(request.MetricCode, true, out var uMetric))
                medal.MetricCode = uMetric;

            if (request.Rules != null)
                medal.Rules = request.Rules;

            if (!string.IsNullOrWhiteSpace(request.Tier))
                medal.Tier = request.Tier.Trim();

            if (request.StageLevel.HasValue && request.StageLevel.Value > 0)
                medal.StageLevel = request.StageLevel.Value;

            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                medal.ImageUrl = request.ImageUrl.Trim();

            if (!string.IsNullOrWhiteSpace(request.CriteriaMetric))
                medal.CriteriaMetric = request.CriteriaMetric.Trim();

            if (request.CriteriaThreshold.HasValue && request.CriteriaThreshold.Value > 0)
                medal.CriteriaThreshold = request.CriteriaThreshold.Value;

            if (!string.IsNullOrWhiteSpace(request.CriteriaUnit))
                medal.CriteriaUnit = request.CriteriaUnit.Trim();

            if (request.IsActive.HasValue)
                medal.IsActive = request.IsActive.Value;

            medal.UpdatedAt = DateTime.UtcNow;

            _medalRepo.Update(medal);
            await _medalRepo.SaveChangesAsync();

            var updated = await _medalRepo.GetByIdAsync(id);
            return _mapper.Map<MedalResponse>(updated);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var medal = await _medalRepo.GetByIdAsync(id);
            if (medal == null) return false;

            _medalRepo.Delete(medal);
            await _medalRepo.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<MedalResponse>> ResetToDefaultsAsync()
        {
            var defaults = GetDefaultMedals();
            var defaultCodes = defaults.Select(d => d.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var d in defaults)
            {
                var existing = await _context.Medals.FirstOrDefaultAsync(m => m.Id == d.Id || m.Code == d.Code);
                if (existing == null)
                {
                    d.CreatedAt = DateTime.UtcNow;
                    d.UpdatedAt = DateTime.UtcNow;
                    await _medalRepo.AddAsync(d);
                }
                else
                {
                    existing.Code = d.Code;
                    existing.Title = d.Title;
                    existing.TitleVi = d.TitleVi;
                    existing.Description = d.Description;
                    existing.DescriptionVi = d.DescriptionVi;
                    existing.Roles = d.Roles;
                    existing.ApplicableRoles = d.ApplicableRoles;
                    existing.MetricCode = d.MetricCode;
                    existing.Rules = d.Rules;
                    existing.Tier = d.Tier;
                    existing.StageLevel = d.StageLevel;
                    existing.ImageUrl = d.ImageUrl;
                    existing.CriteriaMetric = d.CriteriaMetric;
                    existing.CriteriaThreshold = d.CriteriaThreshold;
                    existing.CriteriaUnit = d.CriteriaUnit;
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _medalRepo.Update(existing);
                }
            }

            // Deactivate legacy medals not part of canonical 25 medals
            var legacyMedals = await _context.Medals.Where(m => !defaultCodes.Contains(m.Code) && m.IsActive).ToListAsync();
            foreach (var leg in legacyMedals)
            {
                leg.IsActive = false;
                leg.UpdatedAt = DateTime.UtcNow;
                _medalRepo.Update(leg);
            }

            await _medalRepo.SaveChangesAsync();

            return await GetAllAsync();
        }

        public async Task<IEnumerable<MedalDropdownCategoryDto>> GetMedalsDropdownAsync(string? role = null)
        {
            var query = _context.Medals.AsNoTracking().Where(m => m.IsActive);
            var medals = await query.ToListAsync();

            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                medals = medals.Where(m => MedalMatchesRoles(m, new[] { role })).ToList();
            }

            var grouped = medals
                .GroupBy(m => m.MetricCode)
                .OrderBy(g => (int)g.Key);

            var categories = new List<MedalDropdownCategoryDto>();

            foreach (var g in grouped)
            {
                var sample = g.First();
                var (catVi, catEn) = GetMetricCategoryNames(g.Key);

                categories.Add(new MedalDropdownCategoryDto
                {
                    Category = g.Key.ToString(),
                    CategoryName = catVi,
                    CategoryNameEn = catEn,
                    MetricCode = g.Key.ToString(),
                    Rules = sample.Rules,
                    Medals = g.OrderBy(m => m.StageLevel).Select(m => new MedalDropdownItemDto
                    {
                        Id = m.Id,
                        Code = m.Code,
                        Title = m.Title,
                        TitleVi = m.TitleVi,
                        Tier = m.Tier,
                        StageLevel = m.StageLevel,
                        CriteriaThreshold = m.CriteriaThreshold,
                        CriteriaUnit = m.CriteriaUnit,
                        ImageUrl = m.ImageUrl,
                        ApplicableRoles = ParseRoles(m.ApplicableRoles ?? m.Roles)
                    }).ToList()
                });
            }

            return categories;
        }

        public async Task<MedalAnalyticsResponse> GetMedalsAnalyticsAsync()
        {
            var medals = await _context.Medals
                .AsNoTracking()
                .Include(m => m.UserMedals)
                .OrderBy(m => m.MetricCode)
                .ThenBy(m => m.StageLevel)
                .ToListAsync();

            var totalUsers = await _context.Users.CountAsync(u => u.IsActive == true);
            var totalUnlocked = await _context.UserMedals.CountAsync(um => um.IsUnlocked);
            var totalUsersWithMedals = await _context.UserMedals
                .Where(um => um.IsUnlocked)
                .Select(um => um.UserId)
                .Distinct()
                .CountAsync();

            var items = medals.Select(m => new MedalAnalyticsDto
            {
                MedalId = m.Id,
                Code = m.Code,
                Title = m.Title,
                TitleVi = m.TitleVi,
                Tier = m.Tier,
                StageLevel = m.StageLevel,
                MetricCode = m.MetricCode.ToString(),
                CriteriaThreshold = m.CriteriaThreshold,
                CriteriaUnit = m.CriteriaUnit,
                ApplicableRoles = ParseRoles(m.ApplicableRoles ?? m.Roles),
                TotalAwarded = m.UserMedals.Count(um => um.IsUnlocked),
                TotalInProgress = m.UserMedals.Count(um => !um.IsUnlocked && um.CurrentProgress > 0),
                AwardedPercentage = totalUsers > 0 ? Math.Round((double)m.UserMedals.Count(um => um.IsUnlocked) / totalUsers * 100.0, 2) : 0.0
            }).ToList();

            return new MedalAnalyticsResponse
            {
                TotalMedals = medals.Count,
                TotalUsersWithMedals = totalUsersWithMedals,
                TotalMedalsUnlocked = totalUnlocked,
                Items = items
            };
        }

        public async Task<IEnumerable<MedalUserDetailDto>> GetMedalUsersAsync(string medalId)
        {
            var medal = await _context.Medals
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == medalId || m.Code == medalId);

            if (medal == null)
            {
                throw new KeyNotFoundException($"Medal with ID or Code '{medalId}' not found.");
            }

            var userMedals = await _context.UserMedals
                .AsNoTracking()
                .Include(um => um.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .Where(um => um.MedalId == medal.Id && um.IsUnlocked)
                .OrderByDescending(um => um.UnlockedAt ?? um.AwardedAt)
                .ToListAsync();

            return userMedals.Select(um => new MedalUserDetailDto
            {
                UserId = um.UserId,
                FullName = um.User?.FullName ?? string.Empty,
                Email = um.User?.Email ?? string.Empty,
                AvatarUrl = um.User?.AvatarUrl,
                Roles = um.User?.UserRoles?.Select(ur => ur.Role?.Name ?? ur.UserRole1).Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r!).Distinct().ToList() ?? new List<string>(),
                CurrentProgress = um.CurrentProgress,
                CriteriaThreshold = medal.CriteriaThreshold,
                CriteriaUnit = medal.CriteriaUnit,
                IsUnlocked = um.IsUnlocked,
                UnlockedAt = um.UnlockedAt,
                AwardedAt = um.AwardedAt,
                AwardedByAdminId = um.AwardedByAdminId,
                AwardedReason = um.AwardedReason
            });
        }

        public async Task<UserMedalProgressDto> GetUserMedalProgressAsync(int userId, string medalId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var medal = await _context.Medals
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == medalId || m.Code == medalId);

            if (medal == null)
            {
                throw new KeyNotFoundException($"Medal with ID or Code '{medalId}' not found.");
            }

            // Fresh calculation of user metrics
            await EvaluateUserMedalsAsync(userId);

            var userMedal = await _context.UserMedals
                .AsNoTracking()
                .FirstOrDefaultAsync(um => um.UserId == userId && um.MedalId == medal.Id);

            var currentProgress = userMedal?.CurrentProgress ?? 0;
            var isUnlocked = userMedal?.IsUnlocked ?? (currentProgress >= medal.CriteriaThreshold);
            var progressPercentage = medal.CriteriaThreshold > 0
                ? Math.Min(100.0, Math.Round((double)currentProgress / medal.CriteriaThreshold * 100.0, 1))
                : 0.0;

            var userRoles = user.UserRoles
                .Select(ur => ur.Role?.Name ?? ur.UserRole1)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r!)
                .Distinct()
                .ToList();

            var isRoleEligible = MedalMatchesRoles(medal, userRoles);

            return new UserMedalProgressDto
            {
                UserId = user.UserId,
                UserName = user.FullName,
                MedalId = medal.Id,
                Code = medal.Code,
                Title = medal.Title,
                TitleVi = medal.TitleVi,
                Tier = medal.Tier,
                StageLevel = medal.StageLevel,
                MetricCode = medal.MetricCode.ToString(),
                CurrentProgress = currentProgress,
                CriteriaThreshold = medal.CriteriaThreshold,
                CriteriaUnit = medal.CriteriaUnit,
                ProgressPercentage = progressPercentage,
                IsUnlocked = isUnlocked,
                UnlockedAt = userMedal?.UnlockedAt,
                Rules = medal.Rules,
                Email = user.Email,
                UserRoles = userRoles,
                ApplicableRoles = ParseRoles(medal.ApplicableRoles ?? medal.Roles),
                IsRoleEligible = isRoleEligible
            };
        }

        #endregion

        #region User Methods

        public async Task<IEnumerable<UserMedalResponse>> GetMyMedalsAsync(int userId)
        {
            // Evaluate dynamically first to guarantee historical metrics are calculated
            await EvaluateUserMedalsAsync(userId);

            var activeMedals = await _context.Medals
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Roles)
                .ThenBy(m => m.StageLevel)
                .ToListAsync();

            var userMedals = await _userMedalRepo.GetByUserIdWithMedalsAsync(userId);
            var userMedalDict = userMedals.ToDictionary(um => um.MedalId);

            var responseList = new List<UserMedalResponse>();

            foreach (var medal in activeMedals)
            {
                if (userMedalDict.TryGetValue(medal.Id, out var um))
                {
                    var resp = _mapper.Map<UserMedalResponse>(um);
                    responseList.Add(resp);
                }
                else
                {
                    // If no UserMedal record exists yet, return progress 0
                    responseList.Add(new UserMedalResponse
                    {
                        Medal = _mapper.Map<MedalSummaryDto>(medal),
                        CurrentProgress = 0,
                        IsUnlocked = false,
                        ProgressPercentage = 0.0,
                        UnlockedAt = null
                    });
                }
            }

            return responseList
                .OrderByDescending(r => r.IsUnlocked)
                .ThenByDescending(r => r.ProgressPercentage)
                .ThenBy(r => r.Medal?.Tier)
                .ToList();
        }

        public async Task<IEnumerable<UserMedalResponse>> GetUserUnlockedMedalsAsync(int userId)
        {
            var unlockedMedals = await _userMedalRepo.GetUnlockedByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<UserMedalResponse>>(unlockedMedals);
        }

        public async Task<IEnumerable<UserMedalResponse>> GetUserMedalsAsync(int userId, bool includeLocked, int? callerId, bool isAdmin)
        {
            var targetUserExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!targetUserExists)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            if (!includeLocked)
            {
                return await GetUserUnlockedMedalsAsync(userId);
            }

            // If includeLocked is true, caller must be the user themself, an Admin, or a supervising Lecturer
            if (!isAdmin && (!callerId.HasValue || callerId.Value != userId))
            {
                var isSupervisor = callerId.HasValue && await _context.ResearchGroups
                    .AnyAsync(rg => rg.LecturerId == callerId.Value && rg.GroupMembers.Any(gm => gm.StudentId == userId));

                if (!isSupervisor)
                {
                    throw new UnauthorizedAccessException("Not authorized to view locked medals for this user.");
                }
            }

            await EvaluateUserMedalsAsync(userId);

            var activeMedals = await _context.Medals
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Roles)
                .ThenBy(m => m.StageLevel)
                .ToListAsync();

            var userMedals = await _userMedalRepo.GetByUserIdWithMedalsAsync(userId);
            var userMedalDict = userMedals.ToDictionary(um => um.MedalId);

            var responseList = new List<UserMedalResponse>();

            foreach (var medal in activeMedals)
            {
                if (userMedalDict.TryGetValue(medal.Id, out var um))
                {
                    var resp = _mapper.Map<UserMedalResponse>(um);
                    responseList.Add(resp);
                }
                else
                {
                    responseList.Add(new UserMedalResponse
                    {
                        UserId = userId,
                        MedalId = medal.Id,
                        Code = medal.Code,
                        CriteriaThreshold = medal.CriteriaThreshold,
                        CriteriaUnit = medal.CriteriaUnit,
                        CurrentProgress = 0,
                        IsUnlocked = false,
                        ProgressPercentage = 0.0,
                        UnlockedAt = null,
                        Medal = _mapper.Map<MedalSummaryDto>(medal)
                    });
                }
            }

            return responseList
                .OrderByDescending(r => r.IsUnlocked)
                .ThenByDescending(r => r.ProgressPercentage)
                .ThenBy(r => r.Medal?.Tier)
                .ToList();
        }

        public async Task EvaluateUserMedalsAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return;

            // 1. Calculate user metrics across all domain tables
            var publishedPapersCount = await _context.Papers
                .AsNoTracking()
                .CountAsync(p => p.CreatorId == userId &&
                    (p.Status == "Published" || p.Status == "PUBLISHED" || p.Status == "Accepted" || p.Status == "Approved"));

            var orcidConnectedVal = (user.IsOrcidVerified || !string.IsNullOrWhiteSpace(user.OrcidId)) ? 1 : 0;

            var orcidVerifiedPapersCount = await _context.Papers
                .AsNoTracking()
                .CountAsync(p => p.CreatorId == userId &&
                    (p.AuthorshipVerificationStatus == "APPROVED" ||
                     p.AuthorshipVerificationStatus == "VERIFIED" ||
                     p.AuthorshipVerificationStatus == "MANUALLY_VERIFIED" ||
                     p.AuthorshipVerificationStatus == "AUTOMATICALLY_VERIFIED" ||
                     (user.IsOrcidVerified && (p.Status == "Published" || p.Status == "PUBLISHED"))));

            var hostedSeminarsCount = await _context.Seminars
                .AsNoTracking()
                .CountAsync(s => s.OrganizerId == userId &&
                    (s.Status == "Completed" || s.Status == "COMPLETED" || s.EndTime <= DateTime.UtcNow));

            var attendedSeminarsCount = await _context.SeminarParticipants
                .AsNoTracking()
                .CountAsync(sp => sp.UserId == userId &&
                    (sp.FeedbackSubmittedAt != null || !string.IsNullOrWhiteSpace(sp.FeedbackJson) || sp.InvitationStatus == "Accepted"));

            var completedReviewsCount = await _context.DetailedEvaluations
                .AsNoTracking()
                .Where(de => de.ReviewerId == userId)
                .Select(de => de.ReviewRequestId)
                .Distinct()
                .CountAsync();

            var guidedGroupsCount = await _context.ResearchGroups
                .AsNoTracking()
                .CountAsync(rg => rg.LecturerId == userId &&
                    rg.PhasedReports.Any() &&
                    rg.PhasedReports.All(pr => pr.Status == "APPROVED" || pr.Status == "Completed"));

            var flawlessPhasesCount = await _context.PhasedReports
                .AsNoTracking()
                .CountAsync(pr => pr.ResearchGroup != null &&
                    pr.ResearchGroup.GroupMembers.Any(gm => gm.StudentId == userId) &&
                    (pr.Status == "APPROVED" || pr.Status == "Completed"));

            if (flawlessPhasesCount == 0)
            {
                flawlessPhasesCount = await _context.GroupMembers
                    .AsNoTracking()
                    .CountAsync(gm => gm.StudentId == userId);
            }

            // 7 Canonical Metric Engine Metrics
            var seminarHostQualityCount = await _context.SeminarParticipants
                .AsNoTracking()
                .CountAsync(sp => sp.Seminar.OrganizerId == userId &&
                    (sp.Seminar.Status == "Completed" || sp.Seminar.Status == "COMPLETED" || sp.Seminar.EndTime <= DateTime.UtcNow) &&
                    (sp.FeedbackSubmittedAt != null || !string.IsNullOrWhiteSpace(sp.FeedbackJson)));

            var seminarParticipantCount = await _context.SeminarParticipants
                .AsNoTracking()
                .CountAsync(sp => sp.UserId == userId &&
                    (sp.FeedbackSubmittedAt != null || !string.IsNullOrWhiteSpace(sp.FeedbackJson)));

            var userPostIds = await _context.ForumPosts
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => p.ForumPostId)
                .ToListAsync();

            int communityPostReachCount = 0;
            if (userPostIds.Any())
            {
                var postLikes = await _context.ForumPostLikes
                    .AsNoTracking()
                    .CountAsync(l => userPostIds.Contains(l.ForumPostId));
                var postComments = await _context.ForumComments
                    .AsNoTracking()
                    .CountAsync(c => c.ForumPostId.HasValue && userPostIds.Contains(c.ForumPostId.Value));
                communityPostReachCount = postLikes + postComments;
            }

            var postLikesGiven = await _context.ForumPostLikes
                .AsNoTracking()
                .CountAsync(l => l.UserId == userId);
            var commentVotesGiven = await _context.CommentVotes
                .AsNoTracking()
                .CountAsync(v => v.UserId == userId);
            var commentsWritten = await _context.ForumComments
                .AsNoTracking()
                .CountAsync(c => c.UserId == userId);
            int communityEngagementCount = postLikesGiven + commentVotesGiven + commentsWritten;

            var userCommentIds = await _context.ForumComments
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => c.ForumCommentId)
                .ToListAsync();

            int communityTopCommentCount = 0;
            if (userCommentIds.Any())
            {
                var maxVotes = await _context.CommentVotes
                    .AsNoTracking()
                    .Where(v => userCommentIds.Contains(v.ForumCommentId))
                    .GroupBy(v => v.ForumCommentId)
                    .Select(g => g.Count())
                    .OrderByDescending(c => c)
                    .FirstOrDefaultAsync();

                var maxUpvoteCol = await _context.ForumComments
                    .AsNoTracking()
                    .Where(c => c.UserId == userId && c.UpvoteCount.HasValue)
                    .Select(c => c.UpvoteCount!.Value)
                    .OrderByDescending(v => v)
                    .FirstOrDefaultAsync();

                communityTopCommentCount = Math.Max(maxVotes, maxUpvoteCol);
            }

            // 2. Fetch all active medals
            var medals = await _context.Medals
                .AsNoTracking()
                .Where(m => m.IsActive)
                .ToListAsync();

            // 3. Fetch existing UserMedal records for this user
            var existingUserMedals = await _context.UserMedals
                .Where(um => um.UserId == userId)
                .ToDictionaryAsync(um => um.MedalId);

            var newlyUnlockedMedals = new List<Medal>();

            foreach (var medal in medals)
            {
                int currentProgress = medal.MetricCode switch
                {
                    MedalMetricCode.PROLIFIC_AUTHOR => publishedPapersCount,
                    MedalMetricCode.SEMINAR_HOST_QUALITY => seminarHostQualityCount,
                    MedalMetricCode.MASTER_MENTOR => guidedGroupsCount,
                    MedalMetricCode.SEMINAR_PARTICIPANT => seminarParticipantCount,
                    MedalMetricCode.COMMUNITY_POST_REACH => communityPostReachCount,
                    MedalMetricCode.COMMUNITY_ENGAGEMENT => communityEngagementCount,
                    MedalMetricCode.COMMUNITY_TOP_COMMENT => communityTopCommentCount,
                    _ => medal.CriteriaMetric.ToLower().Trim() switch
                    {
                        "published_papers" => publishedPapersCount,
                        "orcid_connected" => orcidConnectedVal,
                        "orcid_verified_papers" => orcidVerifiedPapersCount,
                        "hosted_seminars" => hostedSeminarsCount,
                        "attended_seminars" => attendedSeminarsCount,
                        "completed_reviews" => completedReviewsCount,
                        "guided_groups_completed" => guidedGroupsCount,
                        "flawless_phases" => flawlessPhasesCount,
                        _ => 0
                    }
                };

                if (existingUserMedals.TryGetValue(medal.Id, out var userMedal))
                {
                    if (userMedal.AwardedByAdminId != null || userMedal.IsUnlocked)
                    {
                        userMedal.IsUnlocked = true;
                        userMedal.CurrentProgress = Math.Max(userMedal.CurrentProgress, currentProgress);
                    }
                    else
                    {
                        userMedal.CurrentProgress = currentProgress;
                        if (currentProgress >= medal.CriteriaThreshold)
                        {
                            userMedal.IsUnlocked = true;
                            userMedal.UnlockedAt = DateTime.UtcNow;
                            newlyUnlockedMedals.Add(medal);
                        }
                    }

                    userMedal.UpdatedAt = DateTime.UtcNow;
                    _context.UserMedals.Update(userMedal);
                }
                else
                {
                    var isUnlocked = currentProgress >= medal.CriteriaThreshold;
                    var newUserMedal = new UserMedal
                    {
                        UserId = userId,
                        MedalId = medal.Id,
                        CurrentProgress = currentProgress,
                        IsUnlocked = isUnlocked,
                        UnlockedAt = isUnlocked ? DateTime.UtcNow : null,
                        AwardedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _context.UserMedals.AddAsync(newUserMedal);

                    if (isUnlocked)
                    {
                        newlyUnlockedMedals.Add(medal);
                    }
                }
            }

            // Save user medals updates
            await _context.SaveChangesAsync();

            // 4. Send in-app notification for each newly unlocked medal
            foreach (var medal in newlyUnlockedMedals)
            {
                var notif = new Notification
                {
                    UserId = userId,
                    Message = $"Chúc mừng bạn đã đạt được huy hiệu {medal.TitleVi}!",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationRepo.AddAsync(notif);
            }

            if (newlyUnlockedMedals.Any())
            {
                await _notificationRepo.SaveChangesAsync();
            }
        }

        #endregion

        #region Ticket BE-MEDAL-GRANT-01 Admin Manual Grant & Dev Helpers

        public async Task<(UserMedalResponse Response, bool IsCreated)> GrantMedalAsync(MedalGrantRequest request, int adminId, string adminName)
        {
            if (request.UserId <= 0)
            {
                throw new ArgumentException("Valid UserId is required.");
            }
            if (string.IsNullOrWhiteSpace(request.MedalCode))
            {
                throw new ArgumentException("MedalCode is required.");
            }
            if (request.ForceUnlocked && string.IsNullOrWhiteSpace(request.AwardedReason))
            {
                throw new ArgumentException("AwardedReason is required when forceUnlocked is true.");
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.UserId}' not found.");
            }

            var medalCodeNormalized = request.MedalCode.Trim();
            var medal = await _context.Medals
                .FirstOrDefaultAsync(m => m.Code == medalCodeNormalized || m.Code.ToLower() == medalCodeNormalized.ToLower());

            if (medal == null)
            {
                throw new KeyNotFoundException($"Medal with code '{request.MedalCode}' not found.");
            }

            // Role check: Reject (409 Conflict) if medalCode does not list any of user's current roles
            var userRoles = user.UserRoles
                .Select(ur => ur.Role?.Name ?? ur.UserRole1)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r!)
                .ToList();

            if (!MedalMatchesRoles(medal, userRoles))
            {
                throw new InvalidOperationException($"Medal '{medal.Code}' does not apply to any role possessed by user {user.UserId} (User Roles: {string.Join(", ", userRoles)}).");
            }

            var correlationId = "evt_" + Guid.NewGuid().ToString("N").Substring(0, 12);

            var existing = await _context.UserMedals
                .Include(um => um.Medal)
                .FirstOrDefaultAsync(um => um.UserId == request.UserId && um.MedalId == medal.Id);

            if (existing != null)
            {
                // Idempotent re-grant (returns 200 OK)
                if (request.ForceUnlocked)
                {
                    existing.CurrentProgress = medal.CriteriaThreshold;
                    existing.IsUnlocked = true;
                    if (!existing.UnlockedAt.HasValue)
                    {
                        existing.UnlockedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    existing.CurrentProgress = Math.Max(existing.CurrentProgress, 1);
                }

                existing.AwardedByAdminId = adminId;
                existing.AwardedReason = request.AwardedReason ?? existing.AwardedReason;
                existing.CorrelationId = correlationId;
                existing.UpdatedAt = DateTime.UtcNow;

                _context.UserMedals.Update(existing);
                await _context.SaveChangesAsync();

                await _auditLogService.CreateAsync(new AuditLogCreateRequest
                {
                    AdminId = adminId,
                    AdminName = adminName,
                    Action = "MEDAL_GRANT",
                    Target = "UserMedal",
                    TargetId = existing.Id.ToString(),
                    Details = $"Admin granted medal {medal.Code} to user {user.UserId} ({user.FullName}). ForceUnlocked: {request.ForceUnlocked}. CorrelationId: {correlationId}. Reason: {request.AwardedReason}"
                });

                return (_mapper.Map<UserMedalResponse>(existing), false);
            }

            // First-time grant (returns 201 Created)
            var isUnlocked = request.ForceUnlocked;
            var currentProgress = request.ForceUnlocked ? medal.CriteriaThreshold : 1;

            var newRow = new UserMedal
            {
                UserId = request.UserId,
                MedalId = medal.Id,
                CurrentProgress = currentProgress,
                IsUnlocked = isUnlocked,
                UnlockedAt = isUnlocked ? DateTime.UtcNow : null,
                AwardedAt = DateTime.UtcNow,
                AwardedByAdminId = adminId,
                AwardedReason = request.AwardedReason,
                CorrelationId = correlationId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _context.UserMedals.AddAsync(newRow);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                var raced = await _context.UserMedals
                    .Include(um => um.Medal)
                    .FirstOrDefaultAsync(um => um.UserId == request.UserId && um.MedalId == medal.Id);
                if (raced != null)
                {
                    return (_mapper.Map<UserMedalResponse>(raced), false);
                }
                throw;
            }

            newRow.Medal = medal;

            await _auditLogService.CreateAsync(new AuditLogCreateRequest
            {
                AdminId = adminId,
                AdminName = adminName,
                Action = "MEDAL_GRANT",
                Target = "UserMedal",
                TargetId = newRow.Id.ToString(),
                Details = $"Admin granted medal {medal.Code} to user {user.UserId} ({user.FullName}). ForceUnlocked: {request.ForceUnlocked}. CorrelationId: {correlationId}. Reason: {request.AwardedReason}"
            });

            return (_mapper.Map<UserMedalResponse>(newRow), true);
        }

        public async Task<bool> RevokeGrantedMedalAsync(long userMedalId, int adminId, string adminName)
        {
            var userMedal = await _context.UserMedals
                .Include(um => um.Medal)
                .FirstOrDefaultAsync(um => um.Id == userMedalId);

            if (userMedal == null)
            {
                // Idempotent: repeat calls return 204 No Content
                return true;
            }

            // Ticket rule: Reject (404) if userMedalId does not refer to an admin-granted row
            if (!userMedal.AwardedByAdminId.HasValue)
            {
                throw new KeyNotFoundException($"Medal grant '{userMedalId}' was not granted by an admin and cannot be revoked via this endpoint.");
            }

            var correlationId = "evt_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var medalCode = userMedal.Medal?.Code ?? userMedal.MedalId;

            _context.UserMedals.Remove(userMedal);
            await _context.SaveChangesAsync();

            await _auditLogService.CreateAsync(new AuditLogCreateRequest
            {
                AdminId = adminId,
                AdminName = adminName,
                Action = "MEDAL_REVOKE",
                Target = "UserMedal",
                TargetId = userMedalId.ToString(),
                Details = $"Admin revoked medal {medalCode} (ID: {userMedalId}) for user {userMedal.UserId}. CorrelationId: {correlationId}"
            });

            return true;
        }

        public async Task<MedalDevGrantAllResponse> DevGrantAllByRoleAsync(MedalDevGrantAllRequest request, int adminId, string adminName)
        {
            if (request.UserId <= 0)
            {
                throw new ArgumentException("Valid UserId is required.");
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.UserId}' not found.");
            }

            var userRoles = user.UserRoles
                .Select(ur => ur.Role?.Name ?? ur.UserRole1)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r!)
                .ToList();

            // Resolve primary role
            var primaryRole = userRoles.FirstOrDefault(r =>
                string.Equals(r, "Lecturer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r, "Researcher", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r, "Reviewer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r, "Graduate Student", StringComparison.OrdinalIgnoreCase))
                ?? userRoles.FirstOrDefault()
                ?? "Researcher";

            // Fetch active medals
            var query = _context.Medals.Where(m => m.IsActive).AsQueryable();

            if (!request.IncludePlatinum)
            {
                query = query.Where(m => m.Tier.ToLower() != "platinum");
            }

            if (!string.IsNullOrWhiteSpace(request.TierFilter) && !request.TierFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                var tf = request.TierFilter.Trim().ToLower();
                query = query.Where(m => m.Tier.ToLower() == tf);
            }

            var allCandidates = await query.ToListAsync();
            var matchingMedals = allCandidates
                .Where(m => MedalMatchesRoles(m, new[] { primaryRole }))
                .OrderBy(m => m.StageLevel)
                .ThenBy(m => m.Tier)
                .ToList();

            var correlationId = "evt_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var awardedRows = new List<MedalDevGrantRow>();
            var awardedCount = 0;
            var skippedCount = 0;

            var existingUserMedals = await _context.UserMedals
                .Where(um => um.UserId == request.UserId)
                .ToDictionaryAsync(um => um.MedalId);

            var childAuditRequests = new List<AuditLogCreateRequest>();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var medal in matchingMedals)
                {
                    if (existingUserMedals.TryGetValue(medal.Id, out var existing))
                    {
                        existing.CurrentProgress = medal.CriteriaThreshold;
                        existing.IsUnlocked = true;
                        if (!existing.UnlockedAt.HasValue) existing.UnlockedAt = DateTime.UtcNow;
                        existing.AwardedByAdminId = adminId;
                        existing.AwardedReason = request.AwardedReason ?? "Dev role seeding";
                        existing.CorrelationId = correlationId;
                        existing.UpdatedAt = DateTime.UtcNow;
                        _context.UserMedals.Update(existing);

                        awardedRows.Add(new MedalDevGrantRow
                        {
                            Id = existing.Id,
                            MedalCode = medal.Code,
                            IsUnlocked = true
                        });
                        awardedCount++;

                        childAuditRequests.Add(new AuditLogCreateRequest
                        {
                            AdminId = adminId,
                            AdminName = adminName,
                            Action = "MEDAL_GRANT",
                            Target = "UserMedal",
                            TargetId = existing.Id.ToString(),
                            Details = $"Granted {medal.Code} as part of role seeding (parent: {correlationId})"
                        });
                    }
                    else
                    {
                        var newUm = new UserMedal
                        {
                            UserId = request.UserId,
                            MedalId = medal.Id,
                            CurrentProgress = medal.CriteriaThreshold,
                            IsUnlocked = true,
                            UnlockedAt = DateTime.UtcNow,
                            AwardedAt = DateTime.UtcNow,
                            AwardedByAdminId = adminId,
                            AwardedReason = request.AwardedReason ?? "Dev role seeding",
                            CorrelationId = correlationId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _context.UserMedals.AddAsync(newUm);
                        await _context.SaveChangesAsync();

                        awardedRows.Add(new MedalDevGrantRow
                        {
                            Id = newUm.Id,
                            MedalCode = medal.Code,
                            IsUnlocked = true
                        });
                        awardedCount++;

                        childAuditRequests.Add(new AuditLogCreateRequest
                        {
                            AdminId = adminId,
                            AdminName = adminName,
                            Action = "MEDAL_GRANT",
                            Target = "UserMedal",
                            TargetId = newUm.Id.ToString(),
                            Details = $"Granted {medal.Code} as part of role seeding (parent: {correlationId})"
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // Parent audit log
                await _auditLogService.CreateAsync(new AuditLogCreateRequest
                {
                    AdminId = adminId,
                    AdminName = adminName,
                    Action = "MEDAL_GRANT_ALL_BY_ROLE",
                    Target = "User",
                    TargetId = request.UserId.ToString(),
                    Details = $"Granted all {awardedCount} role medals for role '{primaryRole}' to user {request.UserId} ({user.FullName}) with correlationId: {correlationId}. Reason: {request.AwardedReason}"
                });

                // Child audit logs
                foreach (var childLog in childAuditRequests)
                {
                    await _auditLogService.CreateAsync(childLog);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return new MedalDevGrantAllResponse
            {
                UserId = request.UserId,
                Role = primaryRole,
                AwardedCount = awardedCount,
                SkippedCount = skippedCount,
                Rows = awardedRows,
                CorrelationId = correlationId
            };
        }

        public async Task<MedalDevRevokeAllResponse> DevRevokeAllAsync(int userId, int adminId, string adminName)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("Valid UserId is required.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{userId}' not found.");
            }

            var correlationId = "evt_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var revokedCount = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var adminGrantedRows = await _context.UserMedals
                    .Where(um => um.UserId == userId && um.AwardedByAdminId != null)
                    .ToListAsync();

                revokedCount = adminGrantedRows.Count;
                if (revokedCount > 0)
                {
                    _context.UserMedals.RemoveRange(adminGrantedRows);
                    await _context.SaveChangesAsync();
                }

                await _auditLogService.CreateAsync(new AuditLogCreateRequest
                {
                    AdminId = adminId,
                    AdminName = adminName,
                    Action = "MEDAL_REVOKE_ALL",
                    Target = "User",
                    TargetId = userId.ToString(),
                    Details = $"Revoked all {revokedCount} admin-granted medals for user {userId} ({user.FullName}) with correlationId: {correlationId}"
                });

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return new MedalDevRevokeAllResponse
            {
                UserId = userId,
                RevokedCount = revokedCount,
                CorrelationId = correlationId
            };
        }

        private static bool MedalMatchesRoles(Medal medal, IEnumerable<string> userRoles)
        {
            var rolesJson = !string.IsNullOrWhiteSpace(medal.ApplicableRoles) && medal.ApplicableRoles != "[\"All\"]"
                ? medal.ApplicableRoles
                : medal.Roles;
            return MedalMatchesRoles(rolesJson, userRoles);
        }

        private static bool MedalMatchesRoles(string? rolesJson, IEnumerable<string> userRoles)
        {
            if (string.IsNullOrWhiteSpace(rolesJson)) return false;
            try
            {
                var roles = JsonSerializer.Deserialize<List<string>>(rolesJson, (JsonSerializerOptions?)null);
                if (roles == null || !roles.Any()) return false;
                if (roles.Any(r => string.Equals(r, "All", StringComparison.OrdinalIgnoreCase))) return true;
                return roles.Any(r => userRoles.Any(ur => string.Equals(ur, r, StringComparison.OrdinalIgnoreCase)));
            }
            catch
            {
                if (rolesJson.Contains("All", StringComparison.OrdinalIgnoreCase)) return true;
                return userRoles.Any(ur => rolesJson.Contains(ur, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static (string Vi, string En) GetMetricCategoryNames(MedalMetricCode code)
        {
            return code switch
            {
                MedalMetricCode.PROLIFIC_AUTHOR => ("Tác giả năng suất (Bài báo xuất bản)", "Prolific Author"),
                MedalMetricCode.SEMINAR_HOST_QUALITY => ("Diễn giả xuất sắc (Tổ chức seminar thành công)", "Seminar Host Quality"),
                MedalMetricCode.MASTER_MENTOR => ("Người hướng dẫn tận tâm (Đồ án sinh viên)", "Master Mentor"),
                MedalMetricCode.SEMINAR_PARTICIPANT => ("Người tham gia tích cực (Hội thảo & Feedback)", "Active Seminar Participant"),
                MedalMetricCode.COMMUNITY_POST_REACH => ("Bài viết lan tỏa (Lượt tương tác bài đăng)", "Community Post Reach"),
                MedalMetricCode.COMMUNITY_ENGAGEMENT => ("Thành viên tương tác (Like & Bình luận)", "Community Engagement"),
                MedalMetricCode.COMMUNITY_TOP_COMMENT => ("Bình luận chất lượng (Lượt thích bình luận)", "Community Top Comment"),
                _ => (code.ToString(), code.ToString())
            };
        }

        private static List<string> ParseRoles(string? rolesJson)
        {
            if (string.IsNullOrWhiteSpace(rolesJson)) return new List<string> { "All" };
            try
            {
                if (rolesJson.Trim().StartsWith("["))
                {
                    return JsonSerializer.Deserialize<List<string>>(rolesJson, (JsonSerializerOptions?)null) ?? new List<string> { rolesJson };
                }
                return new List<string> { rolesJson };
            }
            catch
            {
                return new List<string> { rolesJson };
            }
        }

        #endregion

        #region Default Medals Seed Data

        public static List<Medal> GetDefaultMedals()
        {
            return new List<Medal>
            {
                // 1. PROLIFIC_AUTHOR (Researcher, Lecturer)
                new Medal
                {
                    Id = "medal-prolific-1",
                    Code = "PROLIFIC_AUTHOR_BRONZE",
                    Title = "Prolific Author (Bronze)",
                    TitleVi = "Tác giả năng suất (Cấp 1 - Khởi đầu)",
                    Description = "First research paper published on the ARS platform.",
                    DescriptionVi = "Xuất bản thành công bài báo khoa học đầu tiên trên hệ thống.",
                    Roles = "[\"Researcher\",\"Lecturer\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\"]",
                    MetricCode = MedalMetricCode.PROLIFIC_AUTHOR,
                    Rules = "{\"target\":\"papers\",\"statuses\":[\"Published\",\"PUBLISHED\",\"Accepted\",\"Approved\"]}",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "published_papers",
                    CriteriaThreshold = 1,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-prolific-2",
                    Code = "PROLIFIC_AUTHOR_SILVER",
                    Title = "Prolific Author (Silver)",
                    TitleVi = "Tác giả năng suất (Cấp 2 - Bạc)",
                    Description = "Has 5 or more research papers screened and published by Admin.",
                    DescriptionVi = "Có từ 5 bài báo trở lên được Admin phê duyệt và xuất bản.",
                    Roles = "[\"Researcher\",\"Lecturer\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\"]",
                    MetricCode = MedalMetricCode.PROLIFIC_AUTHOR,
                    Rules = "{\"target\":\"papers\",\"statuses\":[\"Published\",\"PUBLISHED\",\"Accepted\",\"Approved\"]}",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1532012164546-f432f2e37b73?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "published_papers",
                    CriteriaThreshold = 5,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-prolific-3",
                    Code = "PROLIFIC_AUTHOR_GOLD",
                    Title = "Prolific Author (Gold)",
                    TitleVi = "Tác giả năng suất (Cấp 3 - Vàng)",
                    Description = "Has 10 or more approved research papers in the catalog.",
                    DescriptionVi = "Có từ 10 bài báo trở lên được xuất bản trong kho nghiên cứu.",
                    Roles = "[\"Researcher\",\"Lecturer\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\"]",
                    MetricCode = MedalMetricCode.PROLIFIC_AUTHOR,
                    Rules = "{\"target\":\"papers\",\"statuses\":[\"Published\",\"PUBLISHED\",\"Accepted\",\"Approved\"]}",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "published_papers",
                    CriteriaThreshold = 10,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-prolific-4",
                    Code = "PROLIFIC_AUTHOR_PLATINUM",
                    Title = "Prolific Author (Platinum)",
                    TitleVi = "Tác giả năng suất (Cấp 4 - Bạch Kim)",
                    Description = "Has 20 or more research publications, establishing top-tier research presence.",
                    DescriptionVi = "Đạt từ 20 bài báo xuất bản, xác lập vị thế nghiên cứu xuất sắc.",
                    Roles = "[\"Researcher\",\"Lecturer\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\"]",
                    MetricCode = MedalMetricCode.PROLIFIC_AUTHOR,
                    Rules = "{\"target\":\"papers\",\"statuses\":[\"Published\",\"PUBLISHED\",\"Accepted\",\"Approved\"]}",
                    Tier = "Platinum",
                    StageLevel = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1507842229451-7f01be7f7396?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "published_papers",
                    CriteriaThreshold = 20,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },

                // 2. SEMINAR_HOST_QUALITY (Lecturer)
                new Medal
                {
                    Id = "medal-host-quality-1",
                    Code = "SEMINAR_HOST_QUALITY_BRONZE",
                    Title = "Seminar Host Quality (Bronze)",
                    TitleVi = "Diễn giả xuất sắc (Cấp 1 - Khởi đầu)",
                    Description = "Hosted successful seminars with at least 10 participant feedbacks submitted.",
                    DescriptionVi = "Tổ chức seminar thành công với từ 10 người tham dự có nộp feedback.",
                    Roles = "[\"Lecturer\"]",
                    ApplicableRoles = "[\"Lecturer\"]",
                    MetricCode = MedalMetricCode.SEMINAR_HOST_QUALITY,
                    Rules = "{\"target\":\"seminar_host\",\"requireFeedback\":true,\"requireCompleted\":true}",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "seminar_host_feedback",
                    CriteriaThreshold = 10,
                    CriteriaUnit = "người tham dự có nộp feedback",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-host-quality-2",
                    Code = "SEMINAR_HOST_QUALITY_SILVER",
                    Title = "Seminar Host Quality (Silver)",
                    TitleVi = "Diễn giả xuất sắc (Cấp 2 - Bạc)",
                    Description = "Hosted successful seminars with at least 25 participant feedbacks submitted.",
                    DescriptionVi = "Tổ chức seminar thành công với từ 25 người tham dự có nộp feedback.",
                    Roles = "[\"Lecturer\"]",
                    ApplicableRoles = "[\"Lecturer\"]",
                    MetricCode = MedalMetricCode.SEMINAR_HOST_QUALITY,
                    Rules = "{\"target\":\"seminar_host\",\"requireFeedback\":true,\"requireCompleted\":true}",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1511578314322-379afb476865?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "seminar_host_feedback",
                    CriteriaThreshold = 25,
                    CriteriaUnit = "người tham dự có nộp feedback",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-host-quality-3",
                    Code = "SEMINAR_HOST_QUALITY_GOLD",
                    Title = "Seminar Host Quality (Gold)",
                    TitleVi = "Diễn giả xuất sắc (Cấp 3 - Vàng)",
                    Description = "Hosted successful seminars with at least 50 participant feedbacks submitted.",
                    DescriptionVi = "Tổ chức seminar thành công với từ 50 người tham dự có nộp feedback.",
                    Roles = "[\"Lecturer\"]",
                    ApplicableRoles = "[\"Lecturer\"]",
                    MetricCode = MedalMetricCode.SEMINAR_HOST_QUALITY,
                    Rules = "{\"target\":\"seminar_host\",\"requireFeedback\":true,\"requireCompleted\":true}",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1524178232363-1fb2b075b655?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "seminar_host_feedback",
                    CriteriaThreshold = 50,
                    CriteriaUnit = "người tham dự có nộp feedback",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-host-quality-4",
                    Code = "SEMINAR_HOST_QUALITY_PLATINUM",
                    Title = "Seminar Host Quality (Platinum)",
                    TitleVi = "Diễn giả xuất sắc (Cấp 4 - Bạch Kim)",
                    Description = "Hosted successful seminars with at least 75 participant feedbacks submitted.",
                    DescriptionVi = "Tổ chức seminar thành công với từ 75 người tham dự có nộp feedback.",
                    Roles = "[\"Lecturer\"]",
                    ApplicableRoles = "[\"Lecturer\"]",
                    MetricCode = MedalMetricCode.SEMINAR_HOST_QUALITY,
                    Rules = "{\"target\":\"seminar_host\",\"requireFeedback\":true,\"requireCompleted\":true}",
                    Tier = "Platinum",
                    StageLevel = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "seminar_host_feedback",
                    CriteriaThreshold = 75,
                    CriteriaUnit = "người tham dự có nộp feedback",
                    IsActive = true
                },

                // 3. MASTER_MENTOR (Lecturer)
                new Medal
                {
                    Id = "medal-mentor-1",
                    Code = "MASTER_MENTOR_BRONZE",
                    Title = "Master Mentor (Bronze)",
                    TitleVi = "Người hướng dẫn tận tâm (Cấp 1 - Khởi đầu)",
                    Description = "Guided 1 student research group through 100% of their milestone phases.",
                    DescriptionVi = "Hướng dẫn 1 nhóm sinh viên hoàn thành 100% các Phase báo cáo tiến độ.",
                    Roles = "[\"Lecturer\"]",
                    ApplicableRoles = "[\"Lecturer\"]",
                    MetricCode = MedalMetricCode.MASTER_MENTOR,
                    Rules = "{\"target\":\"research_groups\",\"requirePhasesComplete\":true,\"minPhasePassRate\":1.0}",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1523240795612-9a054b0db644?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "guided_groups_completed",
                    CriteriaThreshold = 1,
                    CriteriaUnit = "nhóm",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-mentor-2",
                    Code = "MASTER_MENTOR_SILVER",
                    Title = "Master Mentor (Silver)",
                    TitleVi = "Người hướng dẫn tận tâm (Cấp 2 - Bạc)",
                    Description = "Guided at least 3 student research groups through 100% of milestone phases.",
                    DescriptionVi = "Hướng dẫn ít nhất 3 nhóm sinh viên hoàn thành 100% các Phase báo cáo tiến độ.",
                    Roles = "[\"Lecturer\"]",
                    ApplicableRoles = "[\"Lecturer\"]",
                    MetricCode = MedalMetricCode.MASTER_MENTOR,
                    Rules = "{\"target\":\"research_groups\",\"requirePhasesComplete\":true,\"minPhasePassRate\":1.0}",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1577495508048-b635879837f1?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "guided_groups_completed",
                    CriteriaThreshold = 3,
                    CriteriaUnit = "nhóm",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-mentor-3",
                    Code = "MASTER_MENTOR_GOLD",
                    Title = "Master Mentor (Gold)",
                    TitleVi = "Người hướng dẫn tận tâm (Cấp 3 - Vàng)",
                    Description = "Guided at least 5 student research groups successfully through all phases.",
                    DescriptionVi = "Hướng dẫn từ 5 nhóm sinh viên hoàn thành 100% các giai đoạn đạt chuẩn.",
                    Roles = "[\"Lecturer\"]",
                    ApplicableRoles = "[\"Lecturer\"]",
                    MetricCode = MedalMetricCode.MASTER_MENTOR,
                    Rules = "{\"target\":\"research_groups\",\"requirePhasesComplete\":true,\"minPhasePassRate\":1.0}",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "guided_groups_completed",
                    CriteriaThreshold = 5,
                    CriteriaUnit = "nhóm",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-mentor-4",
                    Code = "MASTER_MENTOR_PLATINUM",
                    Title = "Master Mentor (Platinum)",
                    TitleVi = "Người hướng dẫn tận tâm (Cấp 4 - Bạch Kim)",
                    Description = "Guided 10 or more student research groups successfully through all phases.",
                    DescriptionVi = "Hướng dẫn từ 10 nhóm sinh viên hoàn thành xuất sắc các giai đoạn nghiên cứu.",
                    Roles = "[\"Lecturer\"]",
                    ApplicableRoles = "[\"Lecturer\"]",
                    MetricCode = MedalMetricCode.MASTER_MENTOR,
                    Rules = "{\"target\":\"research_groups\",\"requirePhasesComplete\":true,\"minPhasePassRate\":1.0}",
                    Tier = "Platinum",
                    StageLevel = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1531497865144-0464ef8fb9a9?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "guided_groups_completed",
                    CriteriaThreshold = 10,
                    CriteriaUnit = "nhóm",
                    IsActive = true
                },

                // 4. SEMINAR_PARTICIPANT (Researcher, Lecturer, Reviewer, Graduate Student)
                new Medal
                {
                    Id = "medal-student-seminar-1",
                    Code = "SEMINAR_PARTICIPANT_BRONZE",
                    Title = "Seminar Participant (Bronze)",
                    TitleVi = "Người tham gia tích cực (Cấp 1 - Khởi đầu)",
                    Description = "Actively participated in 10 academic seminars and submitted feedback.",
                    DescriptionVi = "Tham gia và gửi phản hồi đóng góp ý kiến cho 10 buổi seminar học thuật.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.SEMINAR_PARTICIPANT,
                    Rules = "{\"target\":\"seminar_participant\",\"requireFeedback\":true}",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1531482615713-2afd69097998?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "attended_seminars_with_feedback",
                    CriteriaThreshold = 10,
                    CriteriaUnit = "buổi",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-student-seminar-2",
                    Code = "SEMINAR_PARTICIPANT_SILVER",
                    Title = "Seminar Participant (Silver)",
                    TitleVi = "Người tham gia tích cực (Cấp 2 - Bạc)",
                    Description = "Actively participated in 30 academic seminars and submitted feedback.",
                    DescriptionVi = "Tham gia và gửi phản hồi đóng góp ý kiến cho 30 buổi seminar học thuật.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.SEMINAR_PARTICIPANT,
                    Rules = "{\"target\":\"seminar_participant\",\"requireFeedback\":true}",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1523240795612-9a054b0db644?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "attended_seminars_with_feedback",
                    CriteriaThreshold = 30,
                    CriteriaUnit = "buổi",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-student-seminar-3",
                    Code = "SEMINAR_PARTICIPANT_GOLD",
                    Title = "Seminar Participant (Gold)",
                    TitleVi = "Người tham gia tích cực (Cấp 3 - Vàng)",
                    Description = "Actively participated in 50 academic seminars and submitted feedback.",
                    DescriptionVi = "Tham gia và gửi phản hồi tích cực cho 50 buổi seminar khoa học.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.SEMINAR_PARTICIPANT,
                    Rules = "{\"target\":\"seminar_participant\",\"requireFeedback\":true}",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "attended_seminars_with_feedback",
                    CriteriaThreshold = 50,
                    CriteriaUnit = "buổi",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-student-seminar-4",
                    Code = "SEMINAR_PARTICIPANT_PLATINUM",
                    Title = "Seminar Participant (Platinum)",
                    TitleVi = "Người tham gia tích cực (Cấp 4 - Bạch Kim)",
                    Description = "Actively participated in 100 or more academic seminars with feedback.",
                    DescriptionVi = "Tham gia và gửi phản hồi tích cực cho từ 100 buổi seminar khoa học trở lên.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.SEMINAR_PARTICIPANT,
                    Rules = "{\"target\":\"seminar_participant\",\"requireFeedback\":true}",
                    Tier = "Platinum",
                    StageLevel = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "attended_seminars_with_feedback",
                    CriteriaThreshold = 100,
                    CriteriaUnit = "buổi",
                    IsActive = true
                },

                // 5. COMMUNITY_POST_REACH (All 4 roles)
                new Medal
                {
                    Id = "medal-post-reach-1",
                    Code = "COMMUNITY_POST_REACH_BRONZE",
                    Title = "Community Post Reach (Bronze)",
                    TitleVi = "Bài viết lan tỏa (Cấp 1 - Khởi đầu)",
                    Description = "Generated 1,000 total likes and comments across published forum posts.",
                    DescriptionVi = "Đạt tổng 1.000 lượt like và bình luận trên các bài đăng diễn đàn.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.COMMUNITY_POST_REACH,
                    Rules = "{\"target\":\"forum_posts_reach\",\"sumMetrics\":[\"likes\",\"comments\"]}",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1557804506-669a67965ba0?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "post_reach",
                    CriteriaThreshold = 1000,
                    CriteriaUnit = "lượt tương tác",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-post-reach-2",
                    Code = "COMMUNITY_POST_REACH_SILVER",
                    Title = "Community Post Reach (Silver)",
                    TitleVi = "Bài viết lan tỏa (Cấp 2 - Bạc)",
                    Description = "Generated 5,000 total likes and comments across published forum posts.",
                    DescriptionVi = "Đạt tổng 5.000 lượt like và bình luận trên các bài đăng diễn đàn.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.COMMUNITY_POST_REACH,
                    Rules = "{\"target\":\"forum_posts_reach\",\"sumMetrics\":[\"likes\",\"comments\"]}",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1557804506-669a67965ba0?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "post_reach",
                    CriteriaThreshold = 5000,
                    CriteriaUnit = "lượt tương tác",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-post-reach-3",
                    Code = "COMMUNITY_POST_REACH_GOLD",
                    Title = "Community Post Reach (Gold)",
                    TitleVi = "Bài viết lan tỏa (Cấp 3 - Vàng)",
                    Description = "Generated 10,000 total likes and comments across published forum posts.",
                    DescriptionVi = "Đạt tổng 10.000 lượt like và bình luận trên các bài đăng diễn đàn.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.COMMUNITY_POST_REACH,
                    Rules = "{\"target\":\"forum_posts_reach\",\"sumMetrics\":[\"likes\",\"comments\"]}",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1557804506-669a67965ba0?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "post_reach",
                    CriteriaThreshold = 10000,
                    CriteriaUnit = "lượt tương tác",
                    IsActive = true
                },

                // 6. COMMUNITY_ENGAGEMENT (All 4 roles)
                new Medal
                {
                    Id = "medal-engagement-1",
                    Code = "COMMUNITY_ENGAGEMENT_BRONZE",
                    Title = "Community Engagement (Bronze)",
                    TitleVi = "Thành viên tương tác (Cấp 1 - Khởi đầu)",
                    Description = "Contributed 5,000 interactions through likes and comments across the community.",
                    DescriptionVi = "Thực hiện 5.000 lượt tương tác (like bài viết, vote bình luận, viết bình luận).",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.COMMUNITY_ENGAGEMENT,
                    Rules = "{\"target\":\"forum_user_activity\",\"sumActions\":[\"post_likes_given\",\"comment_votes_given\",\"comments_written\"]}",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "community_engagement",
                    CriteriaThreshold = 5000,
                    CriteriaUnit = "lượt tương tác",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-engagement-2",
                    Code = "COMMUNITY_ENGAGEMENT_SILVER",
                    Title = "Community Engagement (Silver)",
                    TitleVi = "Thành viên tương tác (Cấp 2 - Bạc)",
                    Description = "Contributed 10,000 interactions through likes and comments across the community.",
                    DescriptionVi = "Thực hiện 10.000 lượt tương tác (like bài viết, vote bình luận, viết bình luận).",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.COMMUNITY_ENGAGEMENT,
                    Rules = "{\"target\":\"forum_user_activity\",\"sumActions\":[\"post_likes_given\",\"comment_votes_given\",\"comments_written\"]}",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "community_engagement",
                    CriteriaThreshold = 10000,
                    CriteriaUnit = "lượt tương tác",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-engagement-3",
                    Code = "COMMUNITY_ENGAGEMENT_GOLD",
                    Title = "Community Engagement (Gold)",
                    TitleVi = "Thành viên tương tác (Cấp 3 - Vàng)",
                    Description = "Contributed 15,000 interactions through likes and comments across the community.",
                    DescriptionVi = "Thực hiện 15.000 lượt tương tác (like bài viết, vote bình luận, viết bình luận).",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.COMMUNITY_ENGAGEMENT,
                    Rules = "{\"target\":\"forum_user_activity\",\"sumActions\":[\"post_likes_given\",\"comment_votes_given\",\"comments_written\"]}",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "community_engagement",
                    CriteriaThreshold = 15000,
                    CriteriaUnit = "lượt tương tác",
                    IsActive = true
                },

                // 7. COMMUNITY_TOP_COMMENT (All 4 roles)
                new Medal
                {
                    Id = "medal-top-comment-1",
                    Code = "COMMUNITY_TOP_COMMENT_BRONZE",
                    Title = "Community Top Comment (Bronze)",
                    TitleVi = "Bình luận chất lượng (Cấp 1 - Khởi đầu)",
                    Description = "Received 100 likes on a single community comment.",
                    DescriptionVi = "Nhận được 100 lượt thích trên 1 bình luận chất lượng.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.COMMUNITY_TOP_COMMENT,
                    Rules = "{\"target\":\"forum_comment_max_likes\",\"metric\":\"max_upvotes_per_comment\"}",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1516251193007-45ef944ab0c6?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "top_comment_likes",
                    CriteriaThreshold = 100,
                    CriteriaUnit = "lượt thích",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-top-comment-2",
                    Code = "COMMUNITY_TOP_COMMENT_SILVER",
                    Title = "Community Top Comment (Silver)",
                    TitleVi = "Bình luận chất lượng (Cấp 2 - Bạc)",
                    Description = "Received 500 likes on a single community comment.",
                    DescriptionVi = "Nhận được 500 lượt thích trên 1 bình luận chất lượng.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.COMMUNITY_TOP_COMMENT,
                    Rules = "{\"target\":\"forum_comment_max_likes\",\"metric\":\"max_upvotes_per_comment\"}",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1516251193007-45ef944ab0c6?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "top_comment_likes",
                    CriteriaThreshold = 500,
                    CriteriaUnit = "lượt thích",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-top-comment-3",
                    Code = "COMMUNITY_TOP_COMMENT_GOLD",
                    Title = "Community Top Comment (Gold)",
                    TitleVi = "Bình luận chất lượng (Cấp 3 - Vàng)",
                    Description = "Received 1,000 likes on a single community comment.",
                    DescriptionVi = "Nhận được 1.000 lượt thích trên 1 bình luận chất lượng xuất sắc.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    ApplicableRoles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    MetricCode = MedalMetricCode.COMMUNITY_TOP_COMMENT,
                    Rules = "{\"target\":\"forum_comment_max_likes\",\"metric\":\"max_upvotes_per_comment\"}",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1516251193007-45ef944ab0c6?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "top_comment_likes",
                    CriteriaThreshold = 1000,
                    CriteriaUnit = "lượt thích",
                    IsActive = true
                }
            };
        }

        #endregion
    }
}
