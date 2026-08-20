using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using AutoMapper;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DetailedEvaluationController : ControllerBase
    {
        private readonly IDetailedEvaluationRepository _repository;
        private readonly IReviewRequestRepository _reviewRequestRepository;
        private readonly ISubFieldRepository _subFieldRepository;
        private readonly IMapper _mapper;

        public DetailedEvaluationController(
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllAsync();
            var response = _mapper.Map<IEnumerable<DetailedEvaluationResponse>>(items);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DetailedEvaluationCreateRequest request)
        {
            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdValue, out var currentUserId))
            {
                return Unauthorized();
            }

            if (!request.ReviewRequestId.HasValue)
            {
                return BadRequest(new { Message = "ReviewRequestId is required." });
            }

            var reviewRequest = await _reviewRequestRepository.GetByIdWithReviewerAsync(
                request.ReviewRequestId.Value);

            if (reviewRequest == null)
            {
                return BadRequest(new { Message = "Review request not found." });
            }

            if (reviewRequest.ReviewerId != currentUserId)
            {
                return Forbid();
            }

            var alreadyExists = await _repository.ExistsAsync(x =>
                x.ReviewRequestId == request.ReviewRequestId.Value);

            if (alreadyExists)
            {
                return Conflict(new
                {
                    Message = "A detailed evaluation already exists for this review request."
                });
            }

            var validationResult = await NormalizeSpecializedEvaluationAsync(
                reviewRequest,
                request.SpecializedEvaluation);

            if (validationResult.Error != null)
            {
                return validationResult.Error;
            }

            request.ReviewerId = currentUserId;
            request.SpecializedEvaluation =
                validationResult.Items ?? new List<SpecializedEvaluationItemRequest>();

            var item = _mapper.Map<DetailedEvaluation>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var response = _mapper.Map<DetailedEvaluationResponse>(item);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            var response = _mapper.Map<DetailedEvaluationResponse>(item);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] DetailedEvaluationUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();

            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdValue, out var currentUserId))
            {
                return Unauthorized();
            }

            if (request.ReviewRequestId.HasValue &&
                item.ReviewRequestId.HasValue &&
                request.ReviewRequestId.Value != item.ReviewRequestId.Value)
            {
                return BadRequest(new
                {
                    Message = "ReviewRequestId cannot be changed."
                });
            }

            var reviewRequestId =
                item.ReviewRequestId ?? request.ReviewRequestId;

            if (!reviewRequestId.HasValue)
            {
                return BadRequest(new
                {
                    Message = "ReviewRequestId is required."
                });
            }

            var reviewRequest =
                await _reviewRequestRepository.GetByIdWithReviewerAsync(
                    reviewRequestId.Value);

            if (reviewRequest == null)
            {
                return BadRequest(new
                {
                    Message = "Review request not found."
                });
            }

            if (reviewRequest.ReviewerId != currentUserId)
            {
                return Forbid();
            }

            if (request.SpecializedEvaluation != null)
            {
                var validationResult =
                    await NormalizeSpecializedEvaluationAsync(
                        reviewRequest,
                        request.SpecializedEvaluation);

                if (validationResult.Error != null)
                {
                    return validationResult.Error;
                }

                request.SpecializedEvaluation = validationResult.Items;
            }

            request.ReviewRequestId = reviewRequestId;
            request.ReviewerId = currentUserId;

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var response = _mapper.Map<DetailedEvaluationResponse>(item);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return Ok(new { Message = "Deleted successfully." });
        }

        private async Task<(
            List<SpecializedEvaluationItemRequest>? Items,
            IActionResult? Error)>
            NormalizeSpecializedEvaluationAsync(
                ReviewRequest reviewRequest,
                List<SpecializedEvaluationItemRequest>? requestedItems)
        {
            if (requestedItems == null)
            {
                return (null, null);
            }

            if (requestedItems.Count == 0)
            {
                return (
                    new List<SpecializedEvaluationItemRequest>(),
                    null);
            }

            if (reviewRequest.Paper == null)
            {
                return (
                    null,
                    BadRequest(new
                    {
                        Message =
                            "The review request does not reference a valid paper."
                    }));
            }

            if (!reviewRequest.Paper.SubFieldId.HasValue)
            {
                return (
                    null,
                    BadRequest(new
                    {
                        Message =
                            "The paper has no SubField assigned, so specialized evaluation criteria cannot be determined."
                    }));
            }

            var subField =
                await _subFieldRepository.GetByIdWithMajorFieldAsync(
                    reviewRequest.Paper.SubFieldId.Value);

            if (subField == null)
            {
                return (
                    null,
                    BadRequest(new
                    {
                        Message = "The paper SubField was not found."
                    }));
            }

            List<GradingRubricCriterionResponse> rubric;

            try
            {
                rubric =
                    string.IsNullOrWhiteSpace(subField.GradingRubric)
                        ? new List<GradingRubricCriterionResponse>()
                        : JsonSerializer.Deserialize<
                            List<GradingRubricCriterionResponse>>(
                                subField.GradingRubric,
                                (JsonSerializerOptions?)null)
                          ?? new List<GradingRubricCriterionResponse>();
            }
            catch (JsonException)
            {
                return (
                    null,
                    StatusCode(
                        500,
                        new
                        {
                            Message =
                                "The SubField grading rubric contains invalid JSON."
                        }));
            }

            if (rubric.Count == 0)
            {
                return (
                    null,
                    BadRequest(new
                    {
                        Message =
                            "No specialized grading rubric is configured for this SubField."
                    }));
            }

            var duplicateCodes =
                requestedItems
                    .GroupBy(
                        x => x.CriterionCode,
                        StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToList();

            if (duplicateCodes.Count > 0)
            {
                return (
                    null,
                    BadRequest(new
                    {
                        Message =
                            "Duplicate specialized evaluation criteria are not allowed.",
                        DuplicateCriterionCodes = duplicateCodes
                    }));
            }

            var normalizedItems =
                new List<SpecializedEvaluationItemRequest>();

            foreach (var requestedItem in requestedItems)
            {
                var rubricCriterion =
                    rubric.FirstOrDefault(x =>
                        string.Equals(
                            x.Code,
                            requestedItem.CriterionCode,
                            StringComparison.OrdinalIgnoreCase));

                if (rubricCriterion == null)
                {
                    return (
                        null,
                        BadRequest(new
                        {
                            Message =
                                $"CriterionCode '{requestedItem.CriterionCode}' is not valid for SubField '{subField.Name}'."
                        }));
                }

                if (requestedItem.Score < 1 ||
                    requestedItem.Score > rubricCriterion.MaxScore)
                {
                    return (
                        null,
                        BadRequest(new
                        {
                            Message =
                                $"Score for criterion '{rubricCriterion.Code}' must be between 1 and {rubricCriterion.MaxScore}."
                        }));
                }

                normalizedItems.Add(
                    new SpecializedEvaluationItemRequest
                    {
                        CriterionCode = rubricCriterion.Code,
                        CriterionTitle = rubricCriterion.Title,
                        MaxScore = rubricCriterion.MaxScore,
                        Score = requestedItem.Score,
                        Notes = requestedItem.Notes,
                        StandardReferences =
                            rubricCriterion.StandardReferences
                            ?? new List<string>()
                    });
            }

            return (normalizedItems, null);
        }
    }
}
