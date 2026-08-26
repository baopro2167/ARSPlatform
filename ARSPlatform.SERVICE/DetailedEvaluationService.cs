using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class DetailedEvaluationService : IDetailedEvaluationService
    {
        private readonly IDetailedEvaluationRepository _repository;
        private readonly IReviewRequestRepository _reviewRequestRepository;
        private readonly ISubFieldRepository _subFieldRepository;
        private readonly IMapper _mapper;

        public DetailedEvaluationService(
            IDetailedEvaluationRepository repository,
            IReviewRequestRepository reviewRequestRepository,
            ISubFieldRepository subFieldRepository,
            IMapper mapper)
        {
            _repository = repository;
            _reviewRequestRepository = reviewRequestRepository;
            _subFieldRepository = subFieldRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<DetailedEvaluationResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<DetailedEvaluationResponse>>(items);
        }

        public async Task<DetailedEvaluationResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null ? null : _mapper.Map<DetailedEvaluationResponse>(item);
        }

        public async Task<DetailedEvaluationResponse> CreateAsync(DetailedEvaluationCreateRequest request, int reviewerId)
        {
            if (!request.ReviewRequestId.HasValue)
            {
                throw new ArgumentException("ReviewRequestId is required.");
            }

            var reviewRequest = await _reviewRequestRepository.GetByIdWithReviewerAsync(request.ReviewRequestId.Value);
            if (reviewRequest == null)
            {
                throw new ArgumentException("Review request not found.");
            }

            if (reviewRequest.ReviewerId != reviewerId)
            {
                throw new UnauthorizedAccessException("You are not authorized to evaluate this review request.");
            }

            var alreadyExists = await _repository.ExistsAsync(x => x.ReviewRequestId == request.ReviewRequestId.Value);
            if (alreadyExists)
            {
                throw new InvalidOperationException("A detailed evaluation already exists for this review request.");
            }

            var normalizedItems = await NormalizeSpecializedEvaluationAsync(reviewRequest, request.SpecializedEvaluation);
            request.ReviewerId = reviewerId;
            request.SpecializedEvaluation = normalizedItems ?? new List<SpecializedEvaluationItemRequest>();

            var item = _mapper.Map<DetailedEvaluation>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            return _mapper.Map<DetailedEvaluationResponse>(item);
        }

        public async Task<DetailedEvaluationResponse?> UpdateAsync(int id, DetailedEvaluationUpdateRequest request, int reviewerId)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return null;

            if (request.ReviewRequestId.HasValue && item.ReviewRequestId.HasValue && request.ReviewRequestId.Value != item.ReviewRequestId.Value)
            {
                throw new ArgumentException("ReviewRequestId cannot be changed.");
            }

            var reviewRequestId = item.ReviewRequestId ?? request.ReviewRequestId;
            if (!reviewRequestId.HasValue)
            {
                throw new ArgumentException("ReviewRequestId is required.");
            }

            var reviewRequest = await _reviewRequestRepository.GetByIdWithReviewerAsync(reviewRequestId.Value);
            if (reviewRequest == null)
            {
                throw new ArgumentException("Review request not found.");
            }

            if (reviewRequest.ReviewerId != reviewerId)
            {
                throw new UnauthorizedAccessException("You are not authorized to evaluate this review request.");
            }

            if (request.SpecializedEvaluation != null)
            {
                request.SpecializedEvaluation = await NormalizeSpecializedEvaluationAsync(reviewRequest, request.SpecializedEvaluation);
            }

            request.ReviewRequestId = reviewRequestId;
            request.ReviewerId = reviewerId;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            return _mapper.Map<DetailedEvaluationResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }

        private async Task<List<SpecializedEvaluationItemRequest>?> NormalizeSpecializedEvaluationAsync(
            ReviewRequest reviewRequest,
            List<SpecializedEvaluationItemRequest>? requestedItems)
        {
            if (requestedItems == null) return null;
            if (requestedItems.Count == 0) return new List<SpecializedEvaluationItemRequest>();

            if (reviewRequest.Paper == null || !reviewRequest.Paper.SubFieldId.HasValue)
            {
                throw new ArgumentException("The review request does not reference a valid paper with an assigned SubField.");
            }

            var subField = await _subFieldRepository.GetByIdWithMajorFieldAsync(reviewRequest.Paper.SubFieldId.Value);
            if (subField == null)
            {
                throw new ArgumentException("The paper SubField was not found.");
            }

            List<GradingRubricCriterionResponse> rubric;
            try
            {
                rubric = string.IsNullOrWhiteSpace(subField.GradingRubric)
                    ? new List<GradingRubricCriterionResponse>()
                    : JsonSerializer.Deserialize<List<GradingRubricCriterionResponse>>(subField.GradingRubric, (JsonSerializerOptions?)null)
                      ?? new List<GradingRubricCriterionResponse>();
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("The SubField grading rubric contains invalid JSON.");
            }

            if (rubric.Count == 0)
            {
                throw new ArgumentException("No specialized grading rubric is configured for this SubField.");
            }

            var duplicateCodes = requestedItems
                .GroupBy(x => x.CriterionCode, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateCodes.Count > 0)
            {
                throw new ArgumentException($"Duplicate specialized evaluation criteria are not allowed: {string.Join(", ", duplicateCodes)}");
            }

            var normalizedItems = new List<SpecializedEvaluationItemRequest>();
            foreach (var requestedItem in requestedItems)
            {
                var rubricCriterion = rubric.FirstOrDefault(x => string.Equals(x.Code, requestedItem.CriterionCode, StringComparison.OrdinalIgnoreCase));
                if (rubricCriterion == null)
                {
                    throw new ArgumentException($"CriterionCode '{requestedItem.CriterionCode}' is not valid for SubField '{subField.Name}'.");
                }

                if (requestedItem.Score < 1 || requestedItem.Score > rubricCriterion.MaxScore)
                {
                    throw new ArgumentException($"Score for criterion '{rubricCriterion.Code}' must be between 1 and {rubricCriterion.MaxScore}.");
                }

                normalizedItems.Add(new SpecializedEvaluationItemRequest
                {
                    CriterionCode = rubricCriterion.Code,
                    CriterionTitle = rubricCriterion.Title,
                    MaxScore = rubricCriterion.MaxScore,
                    Score = requestedItem.Score,
                    Notes = requestedItem.Notes,
                    StandardReferences = rubricCriterion.StandardReferences ?? new List<string>()
                });
            }

            return normalizedItems;
        }
    }
}
