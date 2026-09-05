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

        public MedalService(
            IMedalRepository medalRepo,
            IUserMedalRepository userMedalRepo,
            INotificationRepository notificationRepo,
            AppDbContext context,
            IMapper mapper)
        {
            _medalRepo = medalRepo;
            _userMedalRepo = userMedalRepo;
            _notificationRepo = notificationRepo;
            _context = context;
            _mapper = mapper;
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
            foreach (var d in defaults)
            {
                var existing = await _medalRepo.GetByIdAsync(d.Id);
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
                    existing.Tier = d.Tier;
                    existing.StageLevel = d.StageLevel;
                    existing.CriteriaMetric = d.CriteriaMetric;
                    existing.CriteriaThreshold = d.CriteriaThreshold;
                    existing.CriteriaUnit = d.CriteriaUnit;
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _medalRepo.Update(existing);
                }
            }
            await _medalRepo.SaveChangesAsync();

            return await GetAllAsync();
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
                .ThenBy(r => r.Medal.Tier)
                .ToList();
        }

        public async Task<IEnumerable<UserMedalResponse>> GetUserUnlockedMedalsAsync(int userId)
        {
            var unlockedMedals = await _userMedalRepo.GetUnlockedByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<UserMedalResponse>>(unlockedMedals);
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

            // If graduate student has no group phase yet but attended seminars or participated
            if (flawlessPhasesCount == 0)
            {
                flawlessPhasesCount = await _context.GroupMembers
                    .AsNoTracking()
                    .CountAsync(gm => gm.StudentId == userId);
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
                int currentProgress = 0;
                switch (medal.CriteriaMetric.ToLower().Trim())
                {
                    case "published_papers":
                        currentProgress = publishedPapersCount;
                        break;
                    case "orcid_connected":
                        currentProgress = orcidConnectedVal;
                        break;
                    case "orcid_verified_papers":
                        currentProgress = orcidVerifiedPapersCount;
                        break;
                    case "hosted_seminars":
                        currentProgress = hostedSeminarsCount;
                        break;
                    case "attended_seminars":
                        currentProgress = attendedSeminarsCount;
                        break;
                    case "completed_reviews":
                        currentProgress = completedReviewsCount;
                        break;
                    case "guided_groups_completed":
                        currentProgress = guidedGroupsCount;
                        break;
                    case "flawless_phases":
                        currentProgress = flawlessPhasesCount;
                        break;
                    default:
                        currentProgress = 0;
                        break;
                }

                if (existingUserMedals.TryGetValue(medal.Id, out var userMedal))
                {
                    userMedal.CurrentProgress = currentProgress;
                    if (!userMedal.IsUnlocked && currentProgress >= medal.CriteriaThreshold)
                    {
                        userMedal.IsUnlocked = true;
                        userMedal.UnlockedAt = DateTime.UtcNow;
                        newlyUnlockedMedals.Add(medal);
                    }
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
                        AwardedAt = DateTime.UtcNow
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

        #region Default Medals Seed Data

        public static List<Medal> GetDefaultMedals()
        {
            return new List<Medal>
            {
                // ORCID Scholar Medals
                new Medal
                {
                    Id = "medal-orcid-1",
                    Code = "ORCID_VERIFIED_BRONZE",
                    Title = "ORCID Verified Scholar (Bronze)",
                    TitleVi = "Học giả xác thực ORCID (Cấp 1 - Đồng)",
                    Description = "Successfully connected and verified an international ORCID iD.",
                    DescriptionVi = "Đã liên kết và xác minh định danh khoa học quốc tế ORCID iD thành công.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1579783902614-a3fb3927b675?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "orcid_connected",
                    CriteriaThreshold = 1,
                    CriteriaUnit = "tài khoản",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-orcid-2",
                    Code = "ORCID_VERIFIED_SILVER",
                    Title = "ORCID Verified Scholar (Silver)",
                    TitleVi = "Học giả xác thực ORCID (Cấp 2 - Bạc)",
                    Description = "Verified authorship through ORCID for at least 1 academic paper.",
                    DescriptionVi = "Xác thực quyền tác giả qua ORCID cho ít nhất 1 bài báo nghiên cứu.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "orcid_verified_papers",
                    CriteriaThreshold = 1,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-orcid-3",
                    Code = "ORCID_VERIFIED_GOLD",
                    Title = "ORCID Verified Scholar (Gold)",
                    TitleVi = "Học giả xác thực ORCID (Cấp 3 - Vàng)",
                    Description = "Full public ORCID profile with 3 or more verified scholarly publications.",
                    DescriptionVi = "Hồ sơ ORCID hoàn chỉnh, đồng bộ từ 3 công trình nghiên cứu chính thức trở lên.",
                    Roles = "[\"Researcher\",\"Lecturer\",\"Reviewer\",\"Graduate Student\"]",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1634017839464-5c339ebe3cb4?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "orcid_verified_papers",
                    CriteriaThreshold = 3,
                    CriteriaUnit = "công trình",
                    IsActive = true
                },

                // Prolific Author Medals (Researcher)
                new Medal
                {
                    Id = "medal-prolific-1",
                    Code = "PROLIFIC_AUTHOR_BRONZE",
                    Title = "Prolific Author (Bronze)",
                    TitleVi = "Tác giả năng suất (Cấp 1 - Khởi đầu)",
                    Description = "First research paper published on the ARS platform.",
                    DescriptionVi = "Xuất bản thành công bài báo khoa học đầu tiên trên hệ thống.",
                    Roles = "[\"Researcher\"]",
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
                    Roles = "[\"Researcher\"]",
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
                    Roles = "[\"Researcher\"]",
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
                    Roles = "[\"Researcher\"]",
                    Tier = "Platinum",
                    StageLevel = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1507842229451-7f01be7f7396?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "published_papers",
                    CriteriaThreshold = 20,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },

                // Seminar Host Medals (Researcher & Lecturer)
                new Medal
                {
                    Id = "medal-host-1",
                    Code = "ACADEMIC_HOST_BRONZE",
                    Title = "Academic Host (Bronze)",
                    TitleVi = "Chủ trì Hội thảo (Cấp 1 - Khởi đầu)",
                    Description = "Successfully hosted 1 academic seminar on the platform.",
                    DescriptionVi = "Tổ chức thành công 1 buổi Seminar học thuật trên hệ thống.",
                    Roles = "[\"Researcher\",\"Lecturer\"]",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "hosted_seminars",
                    CriteriaThreshold = 1,
                    CriteriaUnit = "buổi seminar",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-host-2",
                    Code = "ACADEMIC_HOST_SILVER",
                    Title = "Academic Host (Silver)",
                    TitleVi = "Chủ trì Hội thảo (Cấp 2 - Bạc)",
                    Description = "Successfully hosted 3 or more academic seminars on the platform.",
                    DescriptionVi = "Tổ chức thành công từ 3 buổi Seminar học thuật trở lên trên hệ thống.",
                    Roles = "[\"Researcher\",\"Lecturer\"]",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1511578314322-379afb476865?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "hosted_seminars",
                    CriteriaThreshold = 3,
                    CriteriaUnit = "buổi seminar",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-host-3",
                    Code = "ACADEMIC_HOST_GOLD",
                    Title = "Academic Host (Gold)",
                    TitleVi = "Chủ trì Hội thảo (Cấp 3 - Vàng)",
                    Description = "Successfully hosted 5 or more academic seminars with high engagement.",
                    DescriptionVi = "Tổ chức thành công từ 5 buổi Seminar học thuật với điểm đánh giá cao.",
                    Roles = "[\"Researcher\",\"Lecturer\"]",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1524178232363-1fb2b075b655?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "hosted_seminars",
                    CriteriaThreshold = 5,
                    CriteriaUnit = "buổi seminar",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-host-4",
                    Code = "ACADEMIC_HOST_PLATINUM",
                    Title = "Academic Host (Platinum)",
                    TitleVi = "Chủ trì Hội thảo (Cấp 4 - Bạch Kim)",
                    Description = "Successfully hosted 10 or more academic seminars on the platform.",
                    DescriptionVi = "Tổ chức thành công từ 10 buổi Seminar học thuật uy tín trên hệ thống.",
                    Roles = "[\"Researcher\",\"Lecturer\"]",
                    Tier = "Platinum",
                    StageLevel = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "hosted_seminars",
                    CriteriaThreshold = 10,
                    CriteriaUnit = "buổi seminar",
                    IsActive = true
                },

                // Mentor Medals (Lecturer)
                new Medal
                {
                    Id = "medal-mentor-1",
                    Code = "MASTER_MENTOR_BRONZE",
                    Title = "Master Mentor (Bronze)",
                    TitleVi = "Người hướng dẫn tận tâm (Cấp 1 - Khởi đầu)",
                    Description = "Guided 1 student research group through 100% of their milestone phases.",
                    DescriptionVi = "Hướng dẫn 1 nhóm sinh viên hoàn thành 100% các Phase báo cáo tiến độ.",
                    Roles = "[\"Lecturer\"]",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1523240795612-9a054b0db644?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "guided_groups_completed",
                    CriteriaThreshold = 1,
                    CriteriaUnit = "nhóm nghiên cứu",
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
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1577495508048-b635879837f1?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "guided_groups_completed",
                    CriteriaThreshold = 3,
                    CriteriaUnit = "nhóm nghiên cứu",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-mentor-3",
                    Code = "MASTER_MENTOR_GOLD",
                    Title = "Master Mentor (Gold)",
                    TitleVi = "Người hướng dẫn tận tâm (Cấp 3 - Vàng)",
                    Description = "Guided at least 5 student research groups successfully to defense.",
                    DescriptionVi = "Hướng dẫn từ 5 nhóm sinh viên hoàn thành 100% các giai đoạn đạt chuẩn.",
                    Roles = "[\"Lecturer\"]",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "guided_groups_completed",
                    CriteriaThreshold = 5,
                    CriteriaUnit = "nhóm nghiên cứu",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-mentor-4",
                    Code = "MASTER_MENTOR_PLATINUM",
                    Title = "Master Mentor (Platinum)",
                    TitleVi = "Người hướng dẫn tận tâm (Cấp 4 - Bạch Kim)",
                    Description = "Guided 10 or more student research groups successfully to completion.",
                    DescriptionVi = "Hướng dẫn từ 10 nhóm sinh viên hoàn thành xuất sắc các giai đoạn nghiên cứu.",
                    Roles = "[\"Lecturer\"]",
                    Tier = "Platinum",
                    StageLevel = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1531497865144-0464ef8fb9a9?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "guided_groups_completed",
                    CriteriaThreshold = 10,
                    CriteriaUnit = "nhóm nghiên cứu",
                    IsActive = true
                },

                // Reviewer Milestone Medals (Reviewer)
                new Medal
                {
                    Id = "medal-review-1",
                    Code = "REVIEW_MILESTONE_I",
                    Title = "Review Milestone I (Bronze)",
                    TitleVi = "Cột mốc thẩm định I (Cấp 1 - 5 Bài)",
                    Description = "Completed comprehensive evaluation for 5 scientific manuscripts.",
                    DescriptionVi = "Hoàn thành đánh giá và thẩm định 5 bài báo khoa học.",
                    Roles = "[\"Reviewer\"]",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1455390582262-044cdead277a?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "completed_reviews",
                    CriteriaThreshold = 5,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-review-2",
                    Code = "REVIEW_MILESTONE_II",
                    Title = "Review Milestone II (Silver)",
                    TitleVi = "Cột mốc thẩm định II (Cấp 2 - 10 Bài)",
                    Description = "Completed comprehensive evaluation for 10 scientific manuscripts.",
                    DescriptionVi = "Hoàn thành đánh giá và thẩm định 10 bài báo khoa học.",
                    Roles = "[\"Reviewer\"]",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1434030216411-0b793f4b4173?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "completed_reviews",
                    CriteriaThreshold = 10,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-review-3",
                    Code = "REVIEW_MILESTONE_III",
                    Title = "Review Milestone III (Gold)",
                    TitleVi = "Cột mốc thẩm định III (Cấp 3 - 25 Bài)",
                    Description = "Completed comprehensive evaluation for 25 scientific manuscripts.",
                    DescriptionVi = "Hoàn thành đánh giá và thẩm định 25 bài báo khoa học.",
                    Roles = "[\"Reviewer\"]",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1589829545856-d10d557cf95f?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "completed_reviews",
                    CriteriaThreshold = 25,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-review-4",
                    Code = "REVIEW_MILESTONE_IV",
                    Title = "Review Milestone IV (Platinum)",
                    TitleVi = "Cột mốc thẩm định IV (Cấp 4 - 50 Bài)",
                    Description = "Completed comprehensive evaluation for 50 scientific manuscripts.",
                    DescriptionVi = "Hoàn thành đánh giá và thẩm định 50 bài báo khoa học.",
                    Roles = "[\"Reviewer\"]",
                    Tier = "Platinum",
                    StageLevel = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1505664194779-8beaceb93744?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "completed_reviews",
                    CriteriaThreshold = 50,
                    CriteriaUnit = "bài báo",
                    IsActive = true
                },

                // Seminar Participant Medals (Graduate Student)
                new Medal
                {
                    Id = "medal-student-seminar-1",
                    Code = "SEMINAR_PARTICIPANT_BRONZE",
                    Title = "Seminar Participant (Bronze)",
                    TitleVi = "Người tham dự tích cực (Cấp 1 - Khởi đầu)",
                    Description = "Actively participated in 1 academic seminar and submitted feedback.",
                    DescriptionVi = "Tham gia và gửi phản hồi đóng góp ý kiến cho 1 buổi seminar học thuật.",
                    Roles = "[\"Graduate Student\"]",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1531482615713-2afd69097998?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "attended_seminars",
                    CriteriaThreshold = 1,
                    CriteriaUnit = "buổi seminar",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-student-seminar-2",
                    Code = "SEMINAR_PARTICIPANT_SILVER",
                    Title = "Seminar Participant (Silver)",
                    TitleVi = "Người tham dự tích cực (Cấp 2 - Bạc)",
                    Description = "Actively participated in 3 academic seminars and submitted quality feedback.",
                    DescriptionVi = "Tích cực tham gia các buổi seminar học thuật và gửi phản hồi đóng góp ý kiến (3 buổi).",
                    Roles = "[\"Graduate Student\"]",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1523240795612-9a054b0db644?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "attended_seminars",
                    CriteriaThreshold = 3,
                    CriteriaUnit = "buổi seminar",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-student-seminar-3",
                    Code = "SEMINAR_PARTICIPANT_GOLD",
                    Title = "Seminar Participant (Gold)",
                    TitleVi = "Người tham dự tích cực (Cấp 3 - Vàng)",
                    Description = "Actively participated in 5 academic seminars across research domains.",
                    DescriptionVi = "Tham gia và gửi phản hồi tích cực cho 5 buổi seminar khoa học.",
                    Roles = "[\"Graduate Student\"]",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "attended_seminars",
                    CriteriaThreshold = 5,
                    CriteriaUnit = "buổi seminar",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-student-seminar-4",
                    Code = "SEMINAR_PARTICIPANT_PLATINUM",
                    Title = "Seminar Participant (Platinum)",
                    TitleVi = "Người tham dự tích cực (Cấp 4 - Bạch Kim)",
                    Description = "Actively participated in 10 or more academic seminars.",
                    DescriptionVi = "Tham gia và gửi phản hồi tích cực cho từ 10 buổi seminar khoa học trở lên.",
                    Roles = "[\"Graduate Student\"]",
                    Tier = "Platinum",
                    StageLevel = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "attended_seminars",
                    CriteriaThreshold = 10,
                    CriteriaUnit = "buổi seminar",
                    IsActive = true
                },

                // Flawless Progress Medals (Graduate Student)
                new Medal
                {
                    Id = "medal-flawless-1",
                    Code = "FLAWLESS_PROGRESS_BRONZE",
                    Title = "Flawless Progress (Bronze)",
                    TitleVi = "Tiến độ hoàn hảo (Cấp 1 - Khởi đầu)",
                    Description = "Completed Phase 1 on time without any report rejection.",
                    DescriptionVi = "Nhóm hoàn thành Phase 1 đúng thời hạn và đạt chuẩn không bị từ chối.",
                    Roles = "[\"Graduate Student\"]",
                    Tier = "Bronze",
                    StageLevel = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "flawless_phases",
                    CriteriaThreshold = 1,
                    CriteriaUnit = "phase",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-flawless-2",
                    Code = "FLAWLESS_PROGRESS_SILVER",
                    Title = "Flawless Progress (Silver)",
                    TitleVi = "Tiến độ hoàn hảo (Cấp 2 - Nửa chặng đường)",
                    Description = "Completed 3 consecutive phases on time without extension or rejection.",
                    DescriptionVi = "Nhóm hoàn thành từ 3 Phase liên tiếp đúng hạn và đạt Pass ngay lần đầu.",
                    Roles = "[\"Graduate Student\"]",
                    Tier = "Silver",
                    StageLevel = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "flawless_phases",
                    CriteriaThreshold = 3,
                    CriteriaUnit = "phase",
                    IsActive = true
                },
                new Medal
                {
                    Id = "medal-flawless-3",
                    Code = "FLAWLESS_PROGRESS_GOLD",
                    Title = "Flawless Progress (Gold)",
                    TitleVi = "Tiến độ hoàn hảo (Cấp 3 - Vàng Toàn diện)",
                    Description = "Group completed all research milestone phases without delays or rejections.",
                    DescriptionVi = "Nhóm hoàn thành toàn bộ các giai đoạn mà không lần nào bị trễ hạn hoặc bị từ chối.",
                    Roles = "[\"Graduate Student\"]",
                    Tier = "Gold",
                    StageLevel = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=160&auto=format&fit=crop&q=80",
                    CriteriaMetric = "flawless_phases",
                    CriteriaThreshold = 100,
                    CriteriaUnit = "% giai đoạn",
                    IsActive = true
                }
            };
        }

        #endregion
    }
}
