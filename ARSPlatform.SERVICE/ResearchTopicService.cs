using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using ARSPlatform.REPOSITORIES;

namespace ARSPlatform.SERVICES
{
    public class ResearchTopicService : IResearchTopicService
    {
        private readonly IResearchTopicRepository _repository;
        private readonly ILearningMaterialRepository _learningMaterialRepository;
        private readonly IResearchTopicLearningMaterialRepository _topicMaterialRepository;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IMapper _mapper;

        public ResearchTopicService(
            IResearchTopicRepository repository,
            ILearningMaterialRepository learningMaterialRepository,
            IResearchTopicLearningMaterialRepository topicMaterialRepository,
            IDbContextFactory<AppDbContext> dbContextFactory,
            IMapper mapper)
        {
            _repository = repository;
            _learningMaterialRepository = learningMaterialRepository;
            _topicMaterialRepository = topicMaterialRepository;
            _dbContextFactory = dbContextFactory;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ResearchTopicResponse>> GetAllAsync(int? lecturerId = null)
        {
            Expression<Func<ResearchTopic, bool>>? predicate = lecturerId.HasValue ? x => x.LecturerId == lecturerId.Value : null;
            var items = await _repository.GetAllAsync(predicate, includes: new Expression<Func<ResearchTopic, object>>[]
            {
                x => x.Lecturer!
            });
            return _mapper.Map<IEnumerable<ResearchTopicResponse>>(items);
        }

        public async Task<PagedResult<ResearchTopicResponse>> GetPagedAsync(PaginationParams paginationParams, int? lecturerId = null)
        {
            Expression<Func<ResearchTopic, bool>>? predicate = lecturerId.HasValue ? x => x.LecturerId == lecturerId.Value : null;
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                predicate: predicate,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new Expression<Func<ResearchTopic, object>>[]
                {
                    x => x.Lecturer!
                });
            var dtos = _mapper.Map<List<ResearchTopicResponse>>(paged.Items);
            return new PagedResult<ResearchTopicResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<IEnumerable<ResearchTopicResponse>> GetMyTopicsAsync(int lecturerId)
        {
            return await GetAllAsync(lecturerId);
        }

        public async Task<PagedResult<ResearchTopicResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ResearchTopicResponse?> GetByIdAsync(int id)
        {
            var item = (await _repository.GetAllAsync(x => x.TopicId == id, x => x.Lecturer!)).FirstOrDefault();
            return item == null ? null : _mapper.Map<ResearchTopicResponse>(item);
        }

        public async Task<ResearchTopicResponse> CreateAsync(ResearchTopicCreateRequest request, int? lecturerId = null)
        {
            var item = _mapper.Map<ResearchTopic>(request);
            if (lecturerId.HasValue && !item.LecturerId.HasValue)
            {
                item.LecturerId = lecturerId.Value;
            }
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            var created = await GetByIdAsync(item.TopicId);
            return created ?? _mapper.Map<ResearchTopicResponse>(item);
        }

        public async Task<ResearchTopicResponse?> UpdateAsync(int id, ResearchTopicUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            _mapper.Map(request, item);
            item.UpdatedAt = DateTime.UtcNow;
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            return _mapper.Map<ResearchTopicResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<LearningMaterialResponse>> GetLearningMaterialsByTopicIdAsync(int topicId)
        {
            var topic = await _repository.GetByIdAsync(topicId);
            if (topic == null)
            {
                throw new KeyNotFoundException($"Research topic with ID {topicId} not found.");
            }

            var links = await _topicMaterialRepository.GetAllAsync(
                predicate: x => x.TopicId == topicId,
                includes: new Expression<Func<ResearchTopicLearningMaterial, object>>[]
                {
                    x => x.LearningMaterial!,
                    x => x.LearningMaterial!.Lecturer!,
                    x => x.LearningMaterial!.SubField!
                });

            var materials = links
                .Where(x => x.LearningMaterial != null)
                .Select(x => x.LearningMaterial!)
                .ToList();

            return _mapper.Map<IEnumerable<LearningMaterialResponse>>(materials);
        }

        public async Task<bool> AssignLearningMaterialAsync(int topicId, int learningMaterialId, int currentUserId)
        {
            var topic = await _repository.GetByIdAsync(topicId);
            if (topic == null)
            {
                throw new KeyNotFoundException($"Research topic with ID {topicId} not found.");
            }

            if (topic.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to assign materials to this research topic.");
            }

            var material = await _learningMaterialRepository.GetByIdAsync(learningMaterialId);
            if (material == null)
            {
                throw new KeyNotFoundException($"Learning material with ID {learningMaterialId} not found.");
            }

            if (material.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to assign this learning material because you do not own it.");
            }

            var existing = (await _topicMaterialRepository.GetAllAsync(x => x.TopicId == topicId && x.LearningMaterialId == learningMaterialId)).FirstOrDefault();
            if (existing != null)
            {
                throw new InvalidOperationException("Learning material is already assigned to this topic.");
            }

            var link = new ResearchTopicLearningMaterial
            {
                TopicId = topicId,
                LearningMaterialId = learningMaterialId,
                CreatedAt = DateTime.UtcNow
            };

            await _topicMaterialRepository.AddAsync(link);
            await _topicMaterialRepository.SaveChangesAsync();
            return true;
        }

        public async Task<LearningMaterialResponse> CreateAndAssignLearningMaterialAsync(int topicId, TopicLearningMaterialCreateRequest request, int currentUserId)
        {
            var topic = await _repository.GetByIdAsync(topicId);
            if (topic == null)
            {
                throw new KeyNotFoundException($"Research topic with ID {topicId} not found.");
            }

            if (topic.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to add materials to this research topic.");
            }

            if (string.IsNullOrWhiteSpace(request.FileUrl) ||
                !Uri.TryCreate(request.FileUrl, UriKind.Absolute, out var uriResult) ||
                !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                throw new ArgumentException("FileUrl must be a valid http or https URL.");
            }

            // Dùng IDbContextFactory chỉ để tạo transaction scope
            await using var ctx = await _dbContextFactory.CreateDbContextAsync();
            var strategy = ctx.Database.CreateExecutionStrategy();

            LearningMaterial? createdMaterial = null;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await ctx.Database.BeginTransactionAsync();
                try
                {
                    var localMaterialRepo = new LearningMaterialRepository(ctx);
                    var localTopicMaterialRepo = new ResearchTopicLearningMaterialRepository(ctx);

                    var material = new LearningMaterial
                    {
                        Title = request.Title,
                        FileUrl = request.FileUrl,
                        Description = request.Description,
                        SubFieldId = request.SubFieldId,
                        LecturerId = currentUserId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await localMaterialRepo.AddAsync(material);
                    await localMaterialRepo.SaveChangesAsync();

                    var link = new ResearchTopicLearningMaterial
                    {
                        TopicId = topicId,
                        LearningMaterialId = material.LearningMaterialId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await localTopicMaterialRepo.AddAsync(link);
                    await localTopicMaterialRepo.SaveChangesAsync();

                    await tx.CommitAsync();
                    createdMaterial = material;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            return _mapper.Map<LearningMaterialResponse>(createdMaterial);
        }

        public async Task<bool> RemoveLearningMaterialFromTopicAsync(int topicId, int learningMaterialId, int currentUserId)
        {
            var topic = await _repository.GetByIdAsync(topicId);
            if (topic == null)
            {
                throw new KeyNotFoundException($"Research topic with ID {topicId} not found.");
            }

            if (topic.LecturerId != currentUserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to modify this research topic.");
            }

            var link = (await _topicMaterialRepository.GetAllAsync(x => x.TopicId == topicId && x.LearningMaterialId == learningMaterialId)).FirstOrDefault();
            if (link == null)
            {
                throw new KeyNotFoundException("This learning material is not assigned to this topic.");
            }

            _topicMaterialRepository.Delete(link);
            await _topicMaterialRepository.SaveChangesAsync();
            return true;
        }
    }
}
