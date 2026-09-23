import sys

with open('ARSPlatform.SERVICE/ResearchTopicService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace(
    'private readonly AppDbContext _dbContext;',
    'private readonly Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext> _dbContextFactory;'
)
content = content.replace(
    'AppDbContext dbContext,',
    'Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext> dbContextFactory,'
)
content = content.replace(
    '_dbContext = dbContext;',
    '_dbContextFactory = dbContextFactory;'
)

old_transaction = '''            var strategy = _dbContext.Database.CreateExecutionStrategy();
            LearningMaterial? createdMaterial = null;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    var material = new LearningMaterial
                    {
                        Title = request.Title,
                        FileUrl = request.FileUrl,
                        Description = request.Description,
                        SubFieldId = request.SubFieldId,
                        LecturerId = currentUserId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _dbContext.LearningMaterials.AddAsync(material);
                    await _dbContext.SaveChangesAsync();

                    var link = new ResearchTopicLearningMaterial
                    {
                        TopicId = topicId,
                        LearningMaterialId = material.LearningMaterialId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _dbContext.ResearchTopicLearningMaterials.AddAsync(link);
                    await _dbContext.SaveChangesAsync();

                    await tx.CommitAsync();
                    createdMaterial = material;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });'''

new_transaction = '''            var context = await _dbContextFactory.CreateDbContextAsync();
            var strategy = context.Database.CreateExecutionStrategy();
            LearningMaterial? createdMaterial = null;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await context.Database.BeginTransactionAsync();
                try
                {
                    var material = new LearningMaterial
                    {
                        Title = request.Title,
                        FileUrl = request.FileUrl,
                        Description = request.Description,
                        SubFieldId = request.SubFieldId,
                        LecturerId = currentUserId,
                        CreatedAt = DateTime.UtcNow
                    };

                    var learningRepo = new ARSPlatform.REPOSITORIES.LearningMaterialRepository(context);
                    var topicMaterialRepo = new ARSPlatform.REPOSITORIES.ResearchTopicLearningMaterialRepository(context);

                    await learningRepo.AddAsync(material);
                    await learningRepo.SaveChangesAsync();

                    var link = new ResearchTopicLearningMaterial
                    {
                        TopicId = topicId,
                        LearningMaterialId = material.LearningMaterialId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await topicMaterialRepo.AddAsync(link);
                    await topicMaterialRepo.SaveChangesAsync();

                    await tx.CommitAsync();
                    createdMaterial = material;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });'''

content = content.replace(old_transaction, new_transaction)

with open('ARSPlatform.SERVICE/ResearchTopicService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
