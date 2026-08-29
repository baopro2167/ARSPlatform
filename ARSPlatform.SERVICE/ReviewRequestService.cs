using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class ReviewRequestService : IReviewRequestService
    {
        private readonly IReviewRequestRepository _repository;
        private readonly IPaperRepository _paperRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;

        public ReviewRequestService(
            IReviewRequestRepository repository,
            IPaperRepository paperRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IMapper mapper)
        {
            _repository = repository;
            _paperRepository = paperRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReviewRequestResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllWithReviewerAsync();
            return _mapper.Map<IEnumerable<ReviewRequestResponse>>(items);
        }

        public async Task<PagedResult<ReviewRequestResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                includes: new System.Linq.Expressions.Expression<System.Func<ReviewRequest, object>>[]
                {
                    x => x.Reviewer!,
                    x => x.Paper!
                });
            var dtos = _mapper.Map<List<ReviewRequestResponse>>(paged.Items);
            return new PagedResult<ReviewRequestResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ReviewRequestResponse>> GetByReviewerIdAsync(int reviewerId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByReviewerIdPagedAsync(reviewerId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ReviewRequestResponse>>(paged.Items);
            return new PagedResult<ReviewRequestResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ReviewRequestResponse>> GetByPaperIdAsync(int paperId, int pageNumber, int pageSize)
        {
            var paged = await _repository.GetByPaperIdPagedAsync(paperId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ReviewRequestResponse>>(paged.Items);
            return new PagedResult<ReviewRequestResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ReviewRequestResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ReviewRequestResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdWithReviewerAsync(id);
            return item == null ? null : _mapper.Map<ReviewRequestResponse>(item);
        }

        public async Task<ReviewRequestResponse> CreateAsync(ReviewRequestCreateRequest request)
        {
            var item = _mapper.Map<ReviewRequest>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithReviewerAsync(item.ReviewRequestId);

            if (created != null && created.ReviewerId.HasValue)
            {
                var paperTitle = created.Paper?.Title ?? "Bài báo mới";
                var notification = new Notification
                {
                    UserId = created.ReviewerId.Value,
                    Message = $"Bạn có một bài báo mới được phân công phản biện: \"{paperTitle}\".",
                    IsRead = false,
                    CreatedAt = GetVietnamTime()
                };
                await _notificationRepository.AddAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }

            return _mapper.Map<ReviewRequestResponse>(created ?? item);
        }

        public async Task<ReviewRequestResponse?> UpdateAsync(int id, ReviewRequestUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithReviewerAsync(id);
            return _mapper.Map<ReviewRequestResponse>(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<AutoAssignReviewersResponse> AutoAssignReviewersAsync(AutoAssignReviewersRequest request)
        {
            if (request.ReviewerCount <= 0)
            {
                throw new ArgumentException("Số lượng phản biện viên yêu cầu phải lớn hơn 0.");
            }

            var paper = await _paperRepository.GetByIdAsync(request.PaperId);
            if (paper == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy bài báo với ID {request.PaperId}.");
            }

            if (!paper.SubFieldId.HasValue)
            {
                throw new InvalidOperationException($"Bài báo \"{paper.Title}\" chưa được gán chuyên ngành phụ (SubFieldId) nên không thể tìm kiếm phản biện viên phù hợp.");
            }

            var targetSubFieldId = paper.SubFieldId.Value;
            var nowVn = GetVietnamTime();
            var sevenDaysAgoVn = nowVn.AddDays(-7);
            var sevenDaysAgoUtc = DateTime.UtcNow.AddDays(-7);

            // 1. Lấy danh sách Reviewer đã được gán cho bài báo này trước đó (tránh gán trùng)
            var alreadyAssignedReviewerIds = await _repository.GetQueryable()
                .Where(rr => rr.PaperId == request.PaperId && rr.ReviewerId.HasValue)
                .Select(rr => rr.ReviewerId!.Value)
                .ToListAsync();
            var alreadyAssignedSet = new HashSet<int>(alreadyAssignedReviewerIds);

            // 2. Lấy danh sách Reviewer đã nhận bất kỳ yêu cầu phản biện nào trong vòng 7 ngày gần đây
            var recentlyAssignedReviewerIds = await _repository.GetQueryable()
                .Where(rr => rr.ReviewerId.HasValue && (rr.CreatedAt >= sevenDaysAgoVn || rr.CreatedAt >= sevenDaysAgoUtc))
                .Select(rr => rr.ReviewerId!.Value)
                .Distinct()
                .ToListAsync();
            var recentlyAssignedSet = new HashSet<int>(recentlyAssignedReviewerIds);

            // 3. Lấy số lượng bài đang ở trạng thái 'pending' của từng Reviewer
            var pendingCounts = await _repository.GetQueryable()
                .Where(rr => rr.ReviewerId.HasValue && (rr.Status == "PENDING" || rr.Status == "Pending" || rr.Status == "pending"))
                .GroupBy(rr => rr.ReviewerId!.Value)
                .Select(g => new { ReviewerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ReviewerId, x => x.Count);

            // 4. Tìm kiếm các User có vai trò Reviewer và chuyên ngành phụ (SubFieldId) trùng với bài báo
            var candidateUsers = await _userRepository.GetQueryable()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.ProfessionalProfile)
                .Where(u => u.UserId != paper.CreatorId // Tác giả bài viết không được tự chấm bài mình
                         && (u.IsActive == null || u.IsActive == true)
                         && u.ProfessionalProfile != null
                         && u.ProfessionalProfile.SubFieldId == targetSubFieldId
                         && u.UserRoles.Any(ur => ur.Role != null && (ur.Role.Name == "Reviewer" || ur.UserRole1 == "Reviewer")))
                .ToListAsync();

            // 5. Áp dụng các điều kiện lọc:
            // - Chưa nhận bài này
            // - Không nhận bài trong 7 ngày gần đây
            // - Đang có dưới 3 bài 'pending'
            var eligibleReviewers = candidateUsers
                .Where(u => !alreadyAssignedSet.Contains(u.UserId))
                .Where(u => !recentlyAssignedSet.Contains(u.UserId))
                .Where(u => !pendingCounts.TryGetValue(u.UserId, out var count) || count < 3)
                .OrderBy(u => pendingCounts.TryGetValue(u.UserId, out var count) ? count : 0)
                .Take(request.ReviewerCount)
                .ToList();

            var assignedList = new List<AssignedReviewerDto>();

            // 6. Gán các Reviewer đủ điều kiện vào ReviewRequest và gửi Notification
            foreach (var reviewer in eligibleReviewers)
            {
                var reviewRequest = new ReviewRequest
                {
                    PaperId = paper.PaperId,
                    ReviewerId = reviewer.UserId,
                    Fee = reviewer.ProfessionalProfile?.ReviewFee ?? 0,
                    Status = "PENDING",
                    CreatedAt = nowVn,
                    Airecommended = false,
                    Type = "AutoAssigned"
                };

                await _repository.AddAsync(reviewRequest);
                await _repository.SaveChangesAsync();

                var notification = new Notification
                {
                    UserId = reviewer.UserId,
                    Message = $"Bạn có một yêu cầu phản biện bài báo mới: \"{paper.Title}\".",
                    IsRead = false,
                    CreatedAt = nowVn
                };
                await _notificationRepository.AddAsync(notification);

                assignedList.Add(new AssignedReviewerDto
                {
                    ReviewerId = reviewer.UserId,
                    FullName = reviewer.FullName,
                    Email = reviewer.Email,
                    AvatarUrl = reviewer.AvatarUrl,
                    SubFieldId = reviewer.ProfessionalProfile?.SubFieldId,
                    ReviewFee = reviewer.ProfessionalProfile?.ReviewFee,
                    ReviewRequestId = reviewRequest.ReviewRequestId,
                    Status = "PENDING",
                    CreatedAt = nowVn
                });
            }

            if (assignedList.Any())
            {
                await _notificationRepository.SaveChangesAsync();
            }

            // 7. Tạo thông báo kết quả
            string resultMessage;
            if (assignedList.Count == request.ReviewerCount)
            {
                resultMessage = $"Đã phân công đủ {assignedList.Count} phản biện viên phù hợp cho bài báo \"{paper.Title}\".";
            }
            else if (assignedList.Count > 0)
            {
                resultMessage = $"Đã phân công {assignedList.Count}/{request.ReviewerCount} phản biện viên phù hợp cho bài báo \"{paper.Title}\" (do số lượng phản biện viên đủ điều kiện hiện có chỉ còn {assignedList.Count}).";
            }
            else
            {
                resultMessage = $"Không tìm thấy phản biện viên nào phù hợp hoặc tất cả phản biện viên thuộc chuyên ngành này đều không đủ điều kiện (đã nhận bài trong 7 ngày gần đây hoặc đang có từ 3 bài chờ duyệt).";
            }

            return new AutoAssignReviewersResponse
            {
                PaperId = paper.PaperId,
                PaperTitle = paper.Title,
                SubFieldId = paper.SubFieldId,
                RequestedCount = request.ReviewerCount,
                AssignedCount = assignedList.Count,
                AssignedReviewers = assignedList,
                Message = resultMessage
            };
        }

        private static DateTime GetVietnamTime()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                }
                catch
                {
                    return DateTime.UtcNow.AddHours(7);
                }
            }
        }
    }
}
