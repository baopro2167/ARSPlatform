import os

interfaces = {
    'IMedalRepository': '''
        Task<List<Medal>> GetActiveAsync();
        Task<Medal?> GetActiveByIdAsync(string id);
        Task<int> CountActiveAsync();
        Task<bool> ExistsByIdAsync(string id);
''',
    'IUserMedalRepository': '''
        Task<UserMedal?> GetByUserAndMedalIdAsync(int userId, string medalId);
        Task<List<UserMedal>> GetAllByUserIdAsync(int userId);
        Task<int> CountUnlockedAsync();
        Task<int> CountUnlockedByMedalIdAsync(string medalId);
        Task<List<UserMedal>> GetLeaderboardAsync(string medalId, int topN);
''',
    'IGroupMemberRepository': '''
        Task<bool> IsMemberOrSupervisorAsync(int userId, int groupId);
        Task<int> CountByGroupIdAsync(int groupId);
''',
    'ISeminarParticipantRepository': '''
        Task<int> CountAttendedByUserIdAsync(int userId);
        Task<int> CountHostedByUserIdAsync(int userId);
''',
    'IForumCommentRepository': '''
        Task<int> CountByUserIdAsync(int userId);
        Task<int> GetMaxUpvoteCountByUserIdAsync(int userId);
''',
    'IDetailedEvaluationRepository': '''
        Task<int> CountCompletedByReviewerIdAsync(int userId);
''',
    'IPhasedReportRepository': '''
        Task<int> CountFlawlessByUserIdAsync(int userId);
''',
    'IResearchGroupRepository': '''
        Task<bool> IsSupervisorAsync(int userId, int groupId);
        Task<int> CountGuidedByLecturerIdAsync(int lecturerId);
''',
    'ISeminarRepository': '''
        Task<int> CountHostedByUserIdAsync(int userId);
''',
    'IForumPostRepository': '''
        Task<int> CountLikesByUserIdAsync(int userId);
        Task<int> CountPostsByUserIdAsync(int userId);
        Task<int> GetMaxLikesReceivedByUserIdAsync(int userId);
''',
    'ICommentVoteRepository': '''
        Task<int> CountGivenByUserIdAsync(int userId);
        Task<int> GetMaxVotesOnCommentByUserAsync(int userId);
'''
}

classes = {
    'MedalRepository': '''
        public async Task<List<Medal>> GetActiveAsync() => await _context.Medals.Where(m => m.IsActive).ToListAsync();
        public async Task<Medal?> GetActiveByIdAsync(string id) => await _context.Medals.FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
        public async Task<int> CountActiveAsync() => await _context.Medals.CountAsync(m => m.IsActive);
        public async Task<bool> ExistsByIdAsync(string id) => await _context.Medals.AnyAsync(m => m.Id == id);
''',
    'UserMedalRepository': '''
        public async Task<UserMedal?> GetByUserAndMedalIdAsync(int userId, string medalId) => await _context.UserMedals.Include(um => um.Medal).FirstOrDefaultAsync(um => um.UserId == userId && um.MedalId == medalId);
        public async Task<List<UserMedal>> GetAllByUserIdAsync(int userId) => await _context.UserMedals.Include(um => um.Medal).Where(um => um.UserId == userId).ToListAsync();
        public async Task<int> CountUnlockedAsync() => await _context.UserMedals.CountAsync(um => um.IsUnlocked);
        public async Task<int> CountUnlockedByMedalIdAsync(string medalId) => await _context.UserMedals.CountAsync(um => um.MedalId == medalId && um.IsUnlocked);
        public async Task<List<UserMedal>> GetLeaderboardAsync(string medalId, int topN) => await _context.UserMedals.Include(um => um.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role).Where(um => um.MedalId == medalId).OrderByDescending(um => um.Progress).Take(topN).ToListAsync();
''',
    'GroupMemberRepository': '''
        public async Task<bool> IsMemberOrSupervisorAsync(int userId, int groupId) => await _context.GroupMembers.AnyAsync(gm => gm.UserId == userId && gm.ResearchGroupId == groupId);
        public async Task<int> CountByGroupIdAsync(int groupId) => await _context.GroupMembers.CountAsync(gm => gm.ResearchGroupId == groupId);
''',
    'SeminarParticipantRepository': '''
        public async Task<int> CountAttendedByUserIdAsync(int userId) => await _context.SeminarParticipants.CountAsync(sp => sp.UserId == userId && sp.Status == ARSPlatform.MODEL.Enum.SeminarParticipantStatus.Attended);
        public async Task<int> CountHostedByUserIdAsync(int userId) => await _context.SeminarParticipants.CountAsync(sp => sp.UserId == userId && sp.IsHost);
''',
    'ForumCommentRepository': '''
        public async Task<int> CountByUserIdAsync(int userId) => await _context.ForumComments.CountAsync(fc => fc.UserId == userId);
        public async Task<int> GetMaxUpvoteCountByUserIdAsync(int userId) => await _context.ForumComments.Where(fc => fc.UserId == userId).Select(fc => fc.UpvoteCount).DefaultIfEmpty(0).MaxAsync();
''',
    'DetailedEvaluationRepository': '''
        public async Task<int> CountCompletedByReviewerIdAsync(int userId) => await _context.DetailedEvaluations.CountAsync(de => de.ReviewerId == userId && de.Status == ARSPlatform.MODEL.Enum.DetailedEvaluationStatus.Completed);
''',
    'PhasedReportRepository': '''
        public async Task<int> CountFlawlessByUserIdAsync(int userId) => await _context.PhasedReports.CountAsync(pr => pr.SubmitterId == userId && pr.Score == 100);
''',
    'ResearchGroupRepository': '''
        public async Task<bool> IsSupervisorAsync(int userId, int groupId) => await _context.ResearchGroups.AnyAsync(rg => rg.LecturerId == userId && rg.Id == groupId);
        public async Task<int> CountGuidedByLecturerIdAsync(int lecturerId) => await _context.ResearchGroups.CountAsync(rg => rg.LecturerId == lecturerId);
''',
    'SeminarRepository': '''
        public async Task<int> CountHostedByUserIdAsync(int userId) => await _context.Seminars.CountAsync(s => s.OrganizerId == userId);
''',
    'ForumPostRepository': '''
        public async Task<int> CountLikesByUserIdAsync(int userId) => await _context.ForumPostLikes.CountAsync(fpl => fpl.UserId == userId);
        public async Task<int> CountPostsByUserIdAsync(int userId) => await _context.ForumPosts.CountAsync(fp => fp.UserId == userId);
        public async Task<int> GetMaxLikesReceivedByUserIdAsync(int userId) => await _context.ForumPosts.Where(fp => fp.UserId == userId).Select(fp => fp.Likes).DefaultIfEmpty(0).MaxAsync();
''',
    'CommentVoteRepository': '''
        public async Task<int> CountGivenByUserIdAsync(int userId) => await _context.CommentVotes.CountAsync(cv => cv.UserId == userId);
        public async Task<int> GetMaxVotesOnCommentByUserAsync(int userId) => 0; // fallback
'''
}

for name, content in interfaces.items():
    path = f'd:/CapstoneProject2026/ARSPlatform/ARSPlatform.REPO/Interfaces/{name}.cs'
    with open(path, 'r', encoding='utf-8') as f:
        text = f.read()
    if 'CountActiveAsync' not in text and 'CountByUserIdAsync' not in text:
        text = text.replace('}', content + '\n}')
        with open(path, 'w', encoding='utf-8') as f:
            f.write(text)

for name, content in classes.items():
    path = f'd:/CapstoneProject2026/ARSPlatform/ARSPlatform.REPO/{name}.cs'
    with open(path, 'r', encoding='utf-8') as f:
        text = f.read()
    if 'CountActiveAsync' not in text and 'CountByUserIdAsync' not in text:
        idx = text.rfind('}')
        if idx != -1:
            idx = text.rfind('}', 0, idx)
            text = text[:idx] + content + '\n' + text[idx:]
        with open(path, 'w', encoding='utf-8') as f:
            f.write(text)
print("Done patching repos")
