import sys

with open('ARSPlatform.SERVICE/GroupMemberService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace constructor and fields
content = content.replace(
    'private readonly AppDbContext _dbContext;',
    '''private readonly IResearchGroupRepository _researchGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;'''
)
content = content.replace(
    'public GroupMemberService(IGroupMemberRepository repository, IMapper mapper, AppDbContext dbContext)',
    '''public GroupMemberService(IGroupMemberRepository repository, IMapper mapper, 
            IResearchGroupRepository researchGroupRepository,
            IUserRepository userRepository,
            INotificationRepository notificationRepository)'''
)
content = content.replace(
    '_dbContext = dbContext;',
    '''_researchGroupRepository = researchGroupRepository;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;'''
)

# Replace GetByActivityStatusAsync logic
old_get_by_activity = """            // Repository không có sẵn method này → query trực tiếp qua DbContext
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
                paginationParams.PageSize);"""

new_get_by_activity = """            Expression<Func<GroupMember, bool>> predicate;
            if (groupId.HasValue)
            {
                predicate = x => x.ActivityStatus != null && x.ActivityStatus.ToUpper() == normalizedStatus && x.ResearchGroupId == groupId.Value;
            }
            else
            {
                predicate = x => x.ActivityStatus != null && x.ActivityStatus.ToUpper() == normalizedStatus;
            }

            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate,
                orderBy: q => q.OrderByDescending(x => x.JoinedAt ?? DateTime.MinValue).ThenByDescending(x => x.GroupMemberId),
                includes: new Expression<Func<GroupMember, object>>[] { x => x.Student!, x => x.ResearchGroup! }
            );

            var dtos = _mapper.Map<List<GroupMemberResponse>>(paged.Items);
            return new PagedResult<GroupMemberResponse>(
                dtos,
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize);"""

content = content.replace(old_get_by_activity, new_get_by_activity)

# Replace Notifications
content = content.replace(
    'var group = await _dbContext.ResearchGroups.AsNoTracking().FirstOrDefaultAsync(g => g.ResearchGroupId == item.ResearchGroupId.Value);',
    'var group = await _researchGroupRepository.GetByIdAsync(item.ResearchGroupId.Value);'
)
content = content.replace(
    '''var group = await _dbContext.ResearchGroups
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.ResearchGroupId == item.ResearchGroupId.Value);''',
    'var group = await _researchGroupRepository.GetByIdAsync(item.ResearchGroupId.Value);'
)
content = content.replace(
    'await _dbContext.Notifications.AddAsync(notif);',
    'await _notificationRepository.AddAsync(notif);'
)
content = content.replace(
    'await _dbContext.SaveChangesAsync();',
    'await _notificationRepository.SaveChangesAsync();'
)

# Replace User query
content = content.replace(
    'var student = item.StudentId.HasValue ? await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == item.StudentId.Value) : null;',
    'var student = item.StudentId.HasValue ? await _userRepository.GetByIdAsync(item.StudentId.Value) : null;'
)

with open('ARSPlatform.SERVICE/GroupMemberService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
