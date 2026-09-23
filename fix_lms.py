import sys

with open('ARSPlatform.SERVICE/LearningMaterialService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Usage Constraint Check
content = content.replace('''            var isUsedInTopic = await _dbContext.ResearchTopics.AnyAsync(t =>
                t.GuidanceProjectsUrl != null &&
                (
                    (hasFileUrl && t.GuidanceProjectsUrl.Contains(item.FileUrl!)) ||
                    t.GuidanceProjectsUrl == idString ||
                    t.GuidanceProjectsUrl.Contains($"/materials/{id}") ||
                    t.GuidanceProjectsUrl.Contains($"/learning-materials/{id}") ||
                    t.GuidanceProjectsUrl.Contains($"materialId={id}")
                ));''', '            var isUsedInTopic = await _researchTopicRepository.AnyByLearningMaterialIdAsync(id, item.FileUrl);')

content = content.replace('''            var isUsedInPhasedReport = await _dbContext.PhasedReports.AnyAsync(p =>
                p.PhasedMaterialsUrl != null &&
                (
                    (hasFileUrl && p.PhasedMaterialsUrl.Contains(item.FileUrl!)) ||
                    p.PhasedMaterialsUrl == idString ||
                    p.PhasedMaterialsUrl.Contains($"/materials/{id}") ||
                    p.PhasedMaterialsUrl.Contains($"/learning-materials/{id}") ||
                    p.PhasedMaterialsUrl.Contains($"materialId={id}")
                ));''', '            var isUsedInPhasedReport = await _phasedReportRepository.AnyByLearningMaterialIdAsync(id, item.FileUrl);')

# cascade delete
content = content.replace('''            var relatedShares = await _dbContext.SharedMaterials
                .Include(s => s.Lecturer)
                .Where(s => s.LearningMaterialId == id || s.PaperId == id)
                .ToListAsync();''', '''            var relatedSharesEnum = await _sharedMaterialRepository.GetByLearningMaterialIdAsync(id);
            var relatedShares = relatedSharesEnum.ToList();''')

content = content.replace('var senderUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == item.LecturerId);', 'var senderUser = await _userRepository.GetByIdAsync(item.LecturerId);')

content = content.replace('await _dbContext.Notifications.AddRangeAsync(notifications);', 'await _notificationRepository.AddRangeAsync(notifications);\n                    await _notificationRepository.SaveChangesAsync();')

content = content.replace('_dbContext.SharedMaterials.RemoveRange(relatedShares);', '_sharedMaterialRepository.RemoveRange(relatedShares);\n                await _sharedMaterialRepository.SaveChangesAsync();')

content = content.replace('await _dbContext.SaveChangesAsync();', 'await _repository.SaveChangesAsync();')

with open('ARSPlatform.SERVICE/LearningMaterialService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
