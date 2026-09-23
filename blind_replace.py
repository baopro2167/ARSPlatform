import re
import os

file_path = 'd:/CapstoneProject2026/ARSPlatform/ARSPlatform.SERVICE/MedalService.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

repo_map = {
    'Medals': '_medalRepo',
    'UserMedals': '_userMedalRepo',
    'Users': '_userRepo',
    'Papers': '_paperRepo',
    'Seminars': '_seminarRepo',
    'SeminarParticipants': '_seminarParticipantRepo',
    'DetailedEvaluations': '_detailedEvaluationRepo',
    'ResearchGroups': '_researchGroupRepo',
    'PhasedReports': '_phasedReportRepo',
    'GroupMembers': '_groupMemberRepo',
    'ForumPosts': '_forumPostRepo',
    'ForumPostLikes': '_forumPostRepo', # Mapped to ForumPostRepo for now
    'ForumComments': '_forumCommentRepo',
    'CommentVotes': '_commentVoteRepo',
}

# We replace `_context.Entity.Method(...)` with `_entityRepo.Method(...)`
# E.g. `_context.Users.CountAsync` -> `_userRepo.CountAsync`

def replace_context(match):
    entity = match.group(1)
    if entity in repo_map:
        return repo_map[entity]
    return match.group(0)

# Replace `_context.Entity` with `_entityRepo.GetQueryable()` if it's followed by something else not supported directly,
# but it's easier to just replace `_context.Entity.` with `_entityRepo.` first!
for entity, repo in repo_map.items():
    # e.g. `_context.Medals.` -> `_medalRepo.`
    text = re.sub(rf'_context\.{entity}\.', f'{repo}.', text)
    # also replace `_context.Medals` (without dot, e.g. passing it around) -> `_medalRepo.GetQueryable()`
    text = re.sub(rf'_context\.{entity}(?!\w)', f'{repo}.GetQueryable()', text)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)

print("Done blind replacement")
