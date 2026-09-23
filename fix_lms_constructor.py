import sys
import re

with open('ARSPlatform.SERVICE/LearningMaterialService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace constructor and fields
content = re.sub(
    r'private readonly AppDbContext _dbContext;',
    '''private readonly Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IResearchTopicRepository _researchTopicRepository;
        private readonly ISharedMaterialRepository _sharedMaterialRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IPhasedReportRepository _phasedReportRepository;
        private readonly IUserRepository _userRepository;''',
    content
)

content = re.sub(
    r'AppDbContext dbContext,',
    '''Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext> dbContextFactory,
            IResearchTopicRepository researchTopicRepository,
            ISharedMaterialRepository sharedMaterialRepository,
            INotificationRepository notificationRepository,
            IPhasedReportRepository phasedReportRepository,
            IUserRepository userRepository,''',
    content
)

content = re.sub(
    r'_dbContext = dbContext;',
    '''_dbContextFactory = dbContextFactory;
            _researchTopicRepository = researchTopicRepository;
            _sharedMaterialRepository = sharedMaterialRepository;
            _notificationRepository = notificationRepository;
            _phasedReportRepository = phasedReportRepository;
            _userRepository = userRepository;''',
    content
)

with open('ARSPlatform.SERVICE/LearningMaterialService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
