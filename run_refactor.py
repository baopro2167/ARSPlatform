import os
import re

service_file = 'd:/CapstoneProject2026/ARSPlatform/ARSPlatform.SERVICE/MedalService.cs'
with open(service_file, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace _context with _dbContextFactory and add repos
content = re.sub(
    r'private readonly AppDbContext _context;',
    '''private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IUserRepository _userRepo;
        private readonly IGroupMemberRepository _groupMemberRepo;
        private readonly ISeminarParticipantRepository _seminarParticipantRepo;
        private readonly IForumCommentRepository _forumCommentRepo;
        private readonly ICommentVoteRepository _commentVoteRepo;
        private readonly IDetailedEvaluationRepository _detailedEvaluationRepo;
        private readonly IPhasedReportRepository _phasedReportRepo;
        private readonly IResearchGroupRepository _researchGroupRepo;
        private readonly ISeminarRepository _seminarRepo;
        private readonly IForumPostRepository _forumPostRepo;
        private readonly IPaperRepository _paperRepo;''',
    content
)

content = re.sub(
    r'AppDbContext context,',
    '''IDbContextFactory<AppDbContext> dbContextFactory,
            IUserRepository userRepo,
            IGroupMemberRepository groupMemberRepo,
            ISeminarParticipantRepository seminarParticipantRepo,
            IForumCommentRepository forumCommentRepo,
            ICommentVoteRepository commentVoteRepo,
            IDetailedEvaluationRepository detailedEvaluationRepo,
            IPhasedReportRepository phasedReportRepo,
            IResearchGroupRepository researchGroupRepo,
            ISeminarRepository seminarRepo,
            IForumPostRepository forumPostRepo,
            IPaperRepository paperRepo,''',
    content
)

content = re.sub(
    r'_context = context;',
    '''_dbContextFactory = dbContextFactory;
            _userRepo = userRepo;
            _groupMemberRepo = groupMemberRepo;
            _seminarParticipantRepo = seminarParticipantRepo;
            _forumCommentRepo = forumCommentRepo;
            _commentVoteRepo = commentVoteRepo;
            _detailedEvaluationRepo = detailedEvaluationRepo;
            _phasedReportRepo = phasedReportRepo;
            _researchGroupRepo = researchGroupRepo;
            _seminarRepo = seminarRepo;
            _forumPostRepo = forumPostRepo;
            _paperRepo = paperRepo;''',
    content
)

# Replace usings if needed
if 'using Microsoft.EntityFrameworkCore;' not in content:
    content = 'using Microsoft.EntityFrameworkCore;\n' + content

# Replacements for logic
replacements = [
    (r'_context\.UserMedals\.CountAsync\(\s*um\s*=>\s*um\.MedalId\s*==\s*id\)', r'_userMedalRepo.CountUnlockedByMedalIdAsync(id)'),
    (r'_context\.Database\.BeginTransactionAsync\(\)', r'_dbContextFactory.CreateDbContext().Database.BeginTransactionAsync()'),
    (r'_context\.ChangeTracker\.Clear\(\);', r'// handled by factory'),
    (r'_context\.Medals\.FirstOrDefaultAsync\(\s*m\s*=>\s*m\.Id\s*==\s*d\.Id\s*\|\|\s*m\.Code\s*==\s*d\.Code\)', r'_medalRepo.FindAsync(m => m.Id == d.Id || m.Code == d.Code)'),
    (r'_context\.Medals\.Where\(\s*m\s*=>\s*!defaultCodes\.Contains\(m\.Code\)\s*&&\s*m\.IsActive\)\.ToListAsync\(\)', r'_medalRepo.GetQueryable().Where(m => !defaultCodes.Contains(m.Code) && m.IsActive).ToListAsync()'),
    (r'_context\.Medals\.AsNoTracking\(\)\.Where\(\s*m\s*=>\s*m\.IsActive\)', r'_medalRepo.GetQueryable().AsNoTracking().Where(m => m.IsActive)'),
    (r'_context\.Medals\.Where\(\s*m\s*=>\s*m\.IsActive\)\.AsQueryable\(\)', r'_medalRepo.GetQueryable().Where(m => m.IsActive)'),
    (r'_context\.Users\.CountAsync\(\s*u\s*=>\s*u\.IsActive\s*==\s*true\)', r'_userRepo.CountAsync(u => u.IsActive == true)'),
    (r'_context\.UserMedals\.CountAsync\(\s*um\s*=>\s*um\.IsUnlocked\)', r'_userMedalRepo.CountUnlockedAsync()'),
    (r'_context\.UserMedals\s*\.\s*Where\(\s*um\s*=>\s*um\.IsUnlocked\s*\)\s*\.\s*Select\(\s*um\s*=>\s*um\.UserId\s*\)\s*\.\s*Distinct\(\s*\)\s*\.\s*CountAsync\(\)', r'_userMedalRepo.GetQueryable().Where(um => um.IsUnlocked).Select(um => um.UserId).Distinct().CountAsync()'),
    (r'_context\.Medals\s*\.\s*Include\(\s*m\s*=>\s*m\.UserMedals\s*\)\s*\.\s*Where\(\s*m\s*=>\s*m\.IsActive\s*\)\s*\.\s*OrderBy\(\s*m\s*=>\s*m\.Code\s*\)\s*\.\s*ToListAsync\(\)', r'_medalRepo.GetActiveAsync()'),
    (r'_context\.UserMedals\s*\.\s*Include\(\s*um\s*=>\s*um\.User\s*\)\s*\.\s*ThenInclude\(\s*u\s*=>\s*u\.UserRoles\s*\)\s*\.\s*ThenInclude\(\s*ur\s*=>\s*ur\.Role\s*\)\s*\.\s*Where\(\s*um\s*=>\s*um\.MedalId\s*==\s*medalId\s*\)\s*\.\s*OrderByDescending\(\s*um\s*=>\s*um\.Progress\s*\)\s*\.\s*Take\(\s*limit\s*\)\s*\.\s*ToListAsync\(\)', r'_userMedalRepo.GetLeaderboardAsync(medalId, limit)'),
    (r'_context\.Users\s*\.\s*Include\(\s*u\s*=>\s*u\.UserRoles\s*\)\s*\.\s*ThenInclude\(\s*ur\s*=>\s*ur\.Role\s*\)\s*\.\s*FirstOrDefaultAsync\(\s*u\s*=>\s*u\.UserId\s*==\s*id\s*\)', r'_userRepo.GetWithRoleByIdAsync(id)'),
    (r'_context\.Medals\.FirstOrDefaultAsync\(\s*m\s*=>\s*m\.Id\s*==\s*medalId\)', r'_medalRepo.GetByIdAsync(medalId)'),
    (r'_context\.UserMedals\.FirstOrDefaultAsync\(\s*um\s*=>\s*um\.UserId\s*==\s*userId\s*&&\s*um\.MedalId\s*==\s*medalId\)', r'_userMedalRepo.GetByUserAndMedalIdAsync(userId, medalId)'),
    (r'_context\.Medals\.Where\(\s*m\s*=>\s*m\.IsActive\)\.ToListAsync\(\)', r'_medalRepo.GetActiveAsync()'),
    (r'_context\.Users\.AnyAsync\(\s*u\s*=>\s*u\.UserId\s*==\s*userId\)', r'_userRepo.AnyAsync(u => u.UserId == userId)'),
    (r'_context\.ResearchGroups\.AnyAsync\(\s*rg\s*=>\s*rg\.Id\s*==\s*request\.GroupId\s*&&\s*rg\.LecturerId\s*==\s*callerId\)', r'_researchGroupRepo.IsSupervisorAsync(callerId.Value, request.GroupId.Value)'),
    (r'_context\.Users\.AsNoTracking\(\)\.FirstOrDefaultAsync\(\s*u\s*=>\s*u\.UserId\s*==\s*userId\)', r'_userRepo.FindAsync(u => u.UserId == userId)'),
    (r'_context\.Papers\.CountAsync\(\s*p\s*=>\s*p\.UserId\s*==\s*userId\s*&&\s*p\.Status\s*==\s*PaperStatus\.Published\)', r'_paperRepo.CountAsync(p => p.UserId == userId && p.Status == ARSPlatform.MODEL.Enum.PaperStatus.Published)'),
    (r'_context\.Papers\.CountAsync\(\s*p\s*=>\s*p\.UserId\s*==\s*userId\s*&&\s*p\.IsOrcidVerified\s*==\s*true\)', r'_paperRepo.CountAsync(p => p.UserId == userId && p.IsOrcidVerified == true)'),
    (r'_context\.Seminars\.CountAsync\(\s*s\s*=>\s*s\.OrganizerId\s*==\s*userId\)', r'_seminarRepo.CountHostedByUserIdAsync(userId)'),
    (r'_context\.SeminarParticipants\.CountAsync\(\s*sp\s*=>\s*sp\.UserId\s*==\s*userId\s*&&\s*sp\.Status\s*==\s*SeminarParticipantStatus\.Attended\)', r'_seminarParticipantRepo.CountAttendedByUserIdAsync(userId)'),
    (r'_context\.DetailedEvaluations\.CountAsync\(\s*de\s*=>\s*de\.ReviewerId\s*==\s*userId\s*&&\s*de\.Status\s*==\s*DetailedEvaluationStatus\.Completed\)', r'_detailedEvaluationRepo.CountCompletedByReviewerIdAsync(userId)'),
    (r'_context\.ResearchGroups\.CountAsync\(\s*rg\s*=>\s*rg\.LecturerId\s*==\s*userId\)', r'_researchGroupRepo.CountGuidedByLecturerIdAsync(userId)'),
    (r'_context\.PhasedReports\.CountAsync\(\s*pr\s*=>\s*pr\.SubmitterId\s*==\s*userId\s*&&\s*pr\.Score\s*==\s*100\)', r'_phasedReportRepo.CountFlawlessByUserIdAsync(userId)'),
    (r'_context\.GroupMembers\.CountAsync\(\s*gm\s*=>\s*gm\.UserId\s*==\s*userId\s*&&\s*gm\.Grade\s*==\s*10\)', r'_groupMemberRepo.CountAsync(gm => gm.UserId == userId && gm.Grade == 10)'),
    (r'_context\.SeminarParticipants\.CountAsync\(\s*sp\s*=>\s*sp\.UserId\s*==\s*userId\s*&&\s*sp\.IsHost\)', r'_seminarParticipantRepo.CountHostedByUserIdAsync(userId)'),
    (r'_context\.SeminarParticipants\.CountAsync\(\s*sp\s*=>\s*sp\.SeminarId\s*==\s*seminarId\)', r'_seminarParticipantRepo.CountAsync(sp => sp.SeminarId == seminarId)'),
    (r'_context\.ForumPosts\s*\.\s*Where\(\s*p\s*=>\s*p\.UserId\s*==\s*userId\s*\)\s*\.\s*Select\(\s*p\s*=>\s*p\.Id\s*\)\s*\.\s*ToListAsync\(\)', r'_forumPostRepo.GetQueryable().Where(p => p.UserId == userId).Select(p => p.Id).ToListAsync()'),
    (r'_context\.ForumPostLikes\.CountAsync\(\s*l\s*=>\s*userPostIds\.Contains\(l\.PostId\)\)', r'_forumPostRepo.GetMaxLikesReceivedByUserIdAsync(userId)'), 
    (r'_context\.ForumComments\.CountAsync\(\s*c\s*=>\s*userPostIds\.Contains\(c\.PostId\)\)', r'_forumCommentRepo.CountAsync(c => userPostIds.Contains(c.PostId))'),
    (r'_context\.ForumPostLikes\.CountAsync\(\s*l\s*=>\s*l\.UserId\s*==\s*userId\)', r'_forumPostRepo.CountLikesByUserIdAsync(userId)'),
    (r'_context\.CommentVotes\.CountAsync\(\s*v\s*=>\s*v\.UserId\s*==\s*userId\)', r'_commentVoteRepo.CountGivenByUserIdAsync(userId)'),
    (r'_context\.ForumComments\.CountAsync\(\s*c\s*=>\s*c\.UserId\s*==\s*userId\)', r'_forumCommentRepo.CountByUserIdAsync(userId)'),
    (r'_context\.ForumComments\s*\.\s*Where\(\s*c\s*=>\s*c\.UserId\s*==\s*userId\s*\)\s*\.\s*Select\(\s*c\s*=>\s*c\.Id\s*\)\s*\.\s*ToListAsync\(\)', r'_forumCommentRepo.GetQueryable().Where(c => c.UserId == userId).Select(c => c.Id).ToListAsync()'),
    (r'_context\.CommentVotes\s*\.\s*Where\(\s*v\s*=>\s*userCommentIds\.Contains\(v\.CommentId\)\s*\)\s*\.\s*GroupBy\(\s*v\s*=>\s*v\.CommentId\s*\)\s*\.\s*Select\(\s*g\s*=>\s*g\.Count\(\)\s*\)\s*\.\s*DefaultIfEmpty\(0\)\s*\.\s*MaxAsync\(\)', r'_commentVoteRepo.GetMaxVotesOnCommentByUserAsync(userId)'),
    (r'_context\.ForumComments\s*\.\s*Where\(\s*c\s*=>\s*c\.UserId\s*==\s*userId\s*\)\s*\.\s*Select\(\s*c\s*=>\s*c\.UpvoteCount\s*\)\s*\.\s*DefaultIfEmpty\(0\)\s*\.\s*MaxAsync\(\)', r'_forumCommentRepo.GetMaxUpvoteCountByUserIdAsync(userId)'),
    (r'_context\.Medals\.FirstOrDefaultAsync\(\s*m\s*=>\s*m\.Code\s*==\s*code\)', r'_medalRepo.GetByCodeAsync(code)'),
    (r'_context\.UserMedals\.Where\(\s*um\s*=>\s*um\.UserId\s*==\s*userId\s*\)\.ToListAsync\(\)', r'_userMedalRepo.GetAllByUserIdAsync(userId)'),
    (r'_context\.UserMedals\.Update', r'_userMedalRepo.Update'),
    (r'_context\.UserMedals\.AddAsync', r'_userMedalRepo.AddAsync'),
    (r'_context\.SaveChangesAsync\(\)', r'_userMedalRepo.SaveChangesAsync()'),
    (r'_context\.Users\s*\.\s*Include\(\s*u\s*=>\s*u\.UserRoles\s*\)\s*\.\s*ThenInclude\(\s*ur\s*=>\s*ur\.Role\s*\)\s*\.\s*FirstOrDefaultAsync\(\s*u\s*=>\s*u\.UserId\s*==\s*targetUserId\s*\)', r'_userRepo.GetWithRoleByIdAsync(targetUserId)'),
    (r'_context\.UserMedals\s*\.\s*Include\(\s*um\s*=>\s*um\.Medal\s*\)\s*\.\s*FirstOrDefaultAsync\(\s*um\s*=>\s*um\.UserId\s*==\s*userId\s*&&\s*um\.MedalId\s*==\s*medalId\s*\)', r'_userMedalRepo.GetByUserAndMedalIdAsync(userId, medalId)'),
    (r'_context\.UserMedals\s*\.\s*Include\(\s*um\s*=>\s*um\.Medal\s*\)\s*\.\s*Where\(\s*um\s*=>\s*um\.UserId\s*==\s*userId\s*\)\s*\.\s*ToListAsync\(\)', r'_userMedalRepo.GetAllByUserIdAsync(userId)'),
    (r'_context\.Users\s*\.\s*Include\(\s*u\s*=>\s*u\.UserRoles\s*\)\s*\.\s*ThenInclude\(\s*ur\s*=>\s*ur\.Role\s*\)\s*\.\s*FirstOrDefaultAsync\(\s*u\s*=>\s*u\.UserId\s*==\s*request\.UserId\s*\)', r'_userRepo.GetWithRoleByIdAsync(request.UserId)'),
    (r'_context\.UserMedals\s*\.\s*Where\(\s*um\s*=>\s*um\.UserId\s*==\s*request\.UserId\s*\)\s*\.\s*ToDictionaryAsync\(\s*um\s*=>\s*um\.MedalId\s*\)', r'_userMedalRepo.GetQueryable().Where(um => um.UserId == request.UserId).ToDictionaryAsync(um => um.MedalId)'),
    (r'_context\.UserMedals\s*\.\s*Include\(\s*um\s*=>\s*um\.Medal\s*\)\s*\.\s*FirstOrDefaultAsync\(\s*um\s*=>\s*um\.Id\s*==\s*userMedalId\s*\)', r'_userMedalRepo.GetQueryable().Include(um => um.Medal).FirstOrDefaultAsync(um => um.Id == userMedalId)'),
]

for pat, repl in replacements:
    content = re.sub(pat, repl, content, flags=re.MULTILINE)

# Fallback for remaining _context just in case
content = re.sub(r'_context\.Users\.AsNoTracking', r'_userRepo.GetQueryable().AsNoTracking', content)

with open(service_file, 'w', encoding='utf-8') as f:
    f.write(content)

print("Replacements done.")
