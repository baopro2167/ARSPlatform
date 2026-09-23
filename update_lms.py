import sys

with open('ARSPlatform.SERVICE/LearningMaterialService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace constructor and fields
content = content.replace(
    'private readonly AppDbContext _dbContext;',
    '''private readonly Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IResearchTopicRepository _researchTopicRepository;
        private readonly ISharedMaterialRepository _sharedMaterialRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IPhasedReportRepository _phasedReportRepository;
        private readonly IUserRepository _userRepository;'''
)
content = content.replace(
    'AppDbContext dbContext,',
    '''Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext> dbContextFactory,
            IResearchTopicRepository researchTopicRepository,
            ISharedMaterialRepository sharedMaterialRepository,
            INotificationRepository notificationRepository,
            IPhasedReportRepository phasedReportRepository,
            IUserRepository userRepository,'''
)
content = content.replace(
    '_dbContext = dbContext;',
    '''_dbContextFactory = dbContextFactory;
            _researchTopicRepository = researchTopicRepository;
            _sharedMaterialRepository = sharedMaterialRepository;
            _notificationRepository = notificationRepository;
            _phasedReportRepository = phasedReportRepository;
            _userRepository = userRepository;'''
)

# In CreateAsync
content = content.replace(
    'var topic = await _dbContext.ResearchTopics.FirstOrDefaultAsync(t => t.TopicId == request.TopicId.Value);',
    'var topic = await _researchTopicRepository.GetByIdAsync(request.TopicId.Value);'
)

old_transaction = '''                var strategy = _dbContext.Database.CreateExecutionStrategy();
                LearningMaterial? createdMaterial = null;

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _dbContext.Database.BeginTransactionAsync();
                    try
                    {
                        var item = _mapper.Map<LearningMaterial>(request);
                        item.CreatedAt = DateTime.UtcNow;
                        await _repository.AddAsync(item);
                        await _repository.SaveChangesAsync();

                        var link = new ResearchTopicLearningMaterial
                        {
                            TopicId = request.TopicId.Value,
                            LearningMaterialId = item.LearningMaterialId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _dbContext.ResearchTopicLearningMaterials.AddAsync(link);
                        await _dbContext.SaveChangesAsync();

                        await tx.CommitAsync();
                        createdMaterial = item;
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                });'''

new_transaction = '''                var context = await _dbContextFactory.CreateDbContextAsync();
                var strategy = context.Database.CreateExecutionStrategy();
                LearningMaterial? createdMaterial = null;

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await context.Database.BeginTransactionAsync();
                    try
                    {
                        var item = _mapper.Map<LearningMaterial>(request);
                        item.CreatedAt = DateTime.UtcNow;

                        var localRepo = new ARSPlatform.REPOSITORIES.LearningMaterialRepository(context);
                        var topicMaterialRepo = new ARSPlatform.REPOSITORIES.ResearchTopicLearningMaterialRepository(context);

                        await localRepo.AddAsync(item);
                        await localRepo.SaveChangesAsync();

                        var link = new ResearchTopicLearningMaterial
                        {
                            TopicId = request.TopicId.Value,
                            LearningMaterialId = item.LearningMaterialId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await topicMaterialRepo.AddAsync(link);
                        await topicMaterialRepo.SaveChangesAsync();

                        await tx.CommitAsync();
                        createdMaterial = item;
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                });'''

content = content.replace(old_transaction, new_transaction)

# In DeleteAsync
old_delete = '''            // 1. Usage Constraint Check: Kiểm tra xem tài liệu có đang được liên kết trong ResearchTopic hoặc PhasedReport
            var idString = id.ToString();
            var hasFileUrl = !string.IsNullOrWhiteSpace(item.FileUrl);

            // Kiểm tra ResearchTopic (thông qua GuidanceProjectsUrl)
            var isUsedInTopic = await _dbContext.ResearchTopics.AnyAsync(t =>
                t.GuidanceProjectsUrl != null &&
                (
                    (hasFileUrl && t.GuidanceProjectsUrl.Contains(item.FileUrl!)) ||
                    t.GuidanceProjectsUrl == idString ||
                    t.GuidanceProjectsUrl.Contains($"/materials/{id}") ||
                    t.GuidanceProjectsUrl.Contains($"/learning-materials/{id}") ||
                    t.GuidanceProjectsUrl.Contains($"materialId={id}")
                ));

            if (isUsedInTopic)
            {
                throw new InvalidOperationException("Tài liệu đang được sử dụng trong đề tài nghiên cứu, không thể xóa.");
            }

            // Kiểm tra PhasedReport (thông qua PhasedMaterialsUrl)
            var isUsedInPhasedReport = await _dbContext.PhasedReports.AnyAsync(p =>
                p.PhasedMaterialsUrl != null &&
                (
                    (hasFileUrl && p.PhasedMaterialsUrl.Contains(item.FileUrl!)) ||
                    p.PhasedMaterialsUrl == idString ||
                    p.PhasedMaterialsUrl.Contains($"/materials/{id}") ||
                    p.PhasedMaterialsUrl.Contains($"/learning-materials/{id}") ||
                    p.PhasedMaterialsUrl.Contains($"materialId={id}")
                ));

            if (isUsedInPhasedReport)
            {
                throw new InvalidOperationException("Tài liệu đang được sử dụng trong đề tài nghiên cứu, không thể xóa.");
            }

            // 2. Cascade Delete: Gửi thông báo cho đồng nghiệp đang được share và xóa các bản ghi chia sẻ
            var relatedShares = await _dbContext.SharedMaterials
                .Include(s => s.Lecturer)
                .Where(s => s.LearningMaterialId == id || s.PaperId == id)
                .ToListAsync();

            var revokedSharesCount = relatedShares.Count;
            if (revokedSharesCount > 0)
            {
                var senderUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == item.LecturerId);
                var senderName = senderUser?.FullName ?? item.Lecturer?.FullName ?? "Giảng viên chủ sở hữu";
                var now = DateTime.UtcNow;

                var notifications = new List<Notification>();
                foreach (var s in relatedShares)
                {
                    if (s.SharedWithColleagueId.HasValue)
                    {
                        notifications.Add(new Notification
                        {
                            UserId = s.SharedWithColleagueId.Value,
                            Message = $"Tài liệu \"{item.Title}\" do Giảng viên {senderName} chia sẻ đã bị chủ sở hữu xóa khỏi hệ thống.",
                            IsRead = false,
                            CreatedAt = now
                        });
                    }
                }

                if (notifications.Any())
                {
                    await _dbContext.Notifications.AddRangeAsync(notifications);
                }

                _dbContext.SharedMaterials.RemoveRange(relatedShares);
            }

            // 3. Xóa tài liệu gốc
            _repository.Delete(item);
            await _dbContext.SaveChangesAsync();'''

new_delete = '''            // 1. Usage Constraint Check: Kiểm tra xem tài liệu có đang được liên kết trong ResearchTopic hoặc PhasedReport
            var isUsedInTopic = await _researchTopicRepository.AnyByLearningMaterialIdAsync(id, item.FileUrl);

            if (isUsedInTopic)
            {
                throw new InvalidOperationException("Tài liệu đang được sử dụng trong đề tài nghiên cứu, không thể xóa.");
            }

            var isUsedInPhasedReport = await _phasedReportRepository.AnyByLearningMaterialIdAsync(id, item.FileUrl);

            if (isUsedInPhasedReport)
            {
                throw new InvalidOperationException("Tài liệu đang được sử dụng trong báo cáo giai đoạn, không thể xóa.");
            }

            // 2. Cascade Delete: Gửi thông báo cho đồng nghiệp đang được share và xóa các bản ghi chia sẻ
            var relatedSharesEnum = await _sharedMaterialRepository.GetByLearningMaterialIdAsync(id);
            var relatedShares = relatedSharesEnum.ToList();

            var revokedSharesCount = relatedShares.Count;
            if (revokedSharesCount > 0)
            {
                var senderUser = await _userRepository.GetByIdAsync(item.LecturerId);
                var senderName = senderUser?.FullName ?? item.Lecturer?.FullName ?? "Giảng viên chủ sở hữu";
                var now = DateTime.UtcNow;

                var notifications = new List<Notification>();
                foreach (var s in relatedShares)
                {
                    if (s.SharedWithColleagueId.HasValue)
                    {
                        notifications.Add(new Notification
                        {
                            UserId = s.SharedWithColleagueId.Value,
                            Message = $"Tài liệu \"{item.Title}\" do Giảng viên {senderName} chia sẻ đã bị chủ sở hữu xóa khỏi hệ thống.",
                            IsRead = false,
                            CreatedAt = now
                        });
                    }
                }

                if (notifications.Any())
                {
                    await _notificationRepository.AddRangeAsync(notifications);
                    await _notificationRepository.SaveChangesAsync();
                }

                _sharedMaterialRepository.RemoveRange(relatedShares);
                await _sharedMaterialRepository.SaveChangesAsync();
            }

            // 3. Xóa tài liệu gốc
            _repository.Delete(item);
            await _repository.SaveChangesAsync();'''

content = content.replace(old_delete, new_delete)

with open('ARSPlatform.SERVICE/LearningMaterialService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
