using System.Text.Json;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/PremiumPackage")]
    [Authorize(Roles = "Admin")]
    public class PremiumPackageController : ControllerBase
    {
        private static readonly HashSet<string> AllowedTargetRoles =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "RESEARCHER",
                "REVIEWER",
                "LECTURER"
            };

        private readonly IMembershipPackageRepository _repository;

        public PremiumPackageController(IMembershipPackageRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllAsync();
            var subscriberCounts =
                await _repository.GetSubscriberCountsAsync();

            var response = items
                .OrderBy(x => x.PackageId)
                .Select(x => ToResponse(
                    x,
                    subscriberCounts.GetValueOrDefault(x.PackageId)))
                .ToList();

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] PremiumPackageCreateRequest request)
        {
            var validationError = ValidateCreateRequest(request);
            if (validationError != null)
            {
                return BadRequest(new
                {
                    Message = validationError
                });
            }

            var billingCycle = NormalizeBillingCycle(request.BillingCycle)!;
            var features = NormalizeFeatures(request.Features);
            var now = DateTime.UtcNow;

            var item = new MembershipPackage
            {
                Name = request.Title.Trim(),
                Price = request.PriceVnd,
                DurationDays = GetDurationDays(billingCycle),
                Description = string.Join(Environment.NewLine, features),
                CreatedAt = now,
                TargetRole = request.TargetRole.Trim().ToUpperInvariant(),
                BillingCycle = billingCycle,
                Features = JsonSerializer.Serialize(features),
                IsActive = request.IsActive,
                SubscriberCount = 0,
                UpdatedAt = now
            };

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status201Created,
                ToResponse(item, 0));
        }

        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] PremiumPackageUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var validationError = ValidateUpdateRequest(request);
            if (validationError != null)
            {
                return BadRequest(new
                {
                    Message = validationError
                });
            }

            if (request.Title != null)
            {
                item.Name = request.Title.Trim();
            }

            if (request.TargetRole != null)
            {
                item.TargetRole =
                    request.TargetRole.Trim().ToUpperInvariant();
            }

            if (request.PriceVnd.HasValue)
            {
                item.Price = request.PriceVnd.Value;
            }

            if (request.BillingCycle != null)
            {
                var billingCycle =
                    NormalizeBillingCycle(request.BillingCycle)!;

                item.BillingCycle = billingCycle;
                item.DurationDays = GetDurationDays(billingCycle);
            }

            if (request.Features != null)
            {
                var features = NormalizeFeatures(request.Features);

                item.Features = JsonSerializer.Serialize(features);
                item.Description =
                    string.Join(Environment.NewLine, features);
            }

            if (request.IsActive.HasValue)
            {
                item.IsActive = request.IsActive.Value;
            }

            item.UpdatedAt = DateTime.UtcNow;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var subscriberCount =
                await _repository.GetSubscriberCountAsync(id);

            return Ok(ToResponse(item, subscriberCount));
        }

        [HttpPost("{id:int}/toggle")]
        public async Task<IActionResult> Toggle(
            int id,
            [FromBody] PremiumPackageToggleRequest request)
        {
            if (request == null || !request.IsActive.HasValue)
            {
                return BadRequest(new
                {
                    Message = "isActive is required."
                });
            }

            var item = await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            item.IsActive = request.IsActive.Value;
            item.UpdatedAt = DateTime.UtcNow;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var subscriberCount =
                await _repository.GetSubscriberCountAsync(id);

            return Ok(ToResponse(item, subscriberCount));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var hasPurchaseHistory =
                await _repository.HasPurchaseHistoryAsync(id);

            if (hasPurchaseHistory)
            {
                return Conflict(new
                {
                    Message =
                        "This package has purchase history and cannot be deleted. Set isActive to false instead."
                });
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();

            return NoContent();
        }

        private static string? ValidateCreateRequest(
            PremiumPackageCreateRequest request)
        {
            if (request == null)
            {
                return "Request body is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return "title is required.";
            }

            if (request.Title.Trim().Length > 255)
            {
                return "title cannot exceed 255 characters.";
            }

            if (string.IsNullOrWhiteSpace(request.TargetRole) ||
                !AllowedTargetRoles.Contains(request.TargetRole.Trim()))
            {
                return
                    "targetRole must be RESEARCHER, REVIEWER, or LECTURER.";
            }

            if (request.PriceVnd < 0)
            {
                return "priceVnd cannot be negative.";
            }

            if (NormalizeBillingCycle(request.BillingCycle) == null)
            {
                return "billingCycle must be Monthly or Yearly.";
            }

            return null;
        }

        private static string? ValidateUpdateRequest(
            PremiumPackageUpdateRequest request)
        {
            if (request == null)
            {
                return "Request body is required.";
            }

            if (request.Title != null &&
                string.IsNullOrWhiteSpace(request.Title))
            {
                return "title cannot be empty.";
            }

            if (request.Title?.Trim().Length > 255)
            {
                return "title cannot exceed 255 characters.";
            }

            if (request.TargetRole != null &&
                !AllowedTargetRoles.Contains(request.TargetRole.Trim()))
            {
                return
                    "targetRole must be RESEARCHER, REVIEWER, or LECTURER.";
            }

            if (request.PriceVnd.HasValue &&
                request.PriceVnd.Value < 0)
            {
                return "priceVnd cannot be negative.";
            }

            if (request.BillingCycle != null &&
                NormalizeBillingCycle(request.BillingCycle) == null)
            {
                return "billingCycle must be Monthly or Yearly.";
            }

            return null;
        }

        private static string? NormalizeBillingCycle(
            string? billingCycle)
        {
            if (string.Equals(
                    billingCycle?.Trim(),
                    "Monthly",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Monthly";
            }

            if (string.Equals(
                    billingCycle?.Trim(),
                    "Yearly",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Yearly";
            }

            return null;
        }

        private static int GetDurationDays(string billingCycle)
        {
            return billingCycle == "Yearly" ? 365 : 30;
        }

        private static string[] NormalizeFeatures(
            IEnumerable<string>? features)
        {
            return features?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray()
                ?? Array.Empty<string>();
        }

        private static string[] ReadFeatures(
            MembershipPackage item)
        {
            if (!string.IsNullOrWhiteSpace(item.Features))
            {
                try
                {
                    var features =
                        JsonSerializer.Deserialize<string[]>(item.Features);

                    if (features != null)
                    {
                        return NormalizeFeatures(features);
                    }
                }
                catch (JsonException)
                {
                    // Legacy rows fall back to Description below.
                }
            }

            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return Array.Empty<string>();
            }

            return NormalizeFeatures(
                item.Description.Split(
                    new[] { "\r\n", "\n" },
                    StringSplitOptions.RemoveEmptyEntries));
        }

        private static PremiumPackageResponse ToResponse(
            MembershipPackage item,
            int subscriberCount)
        {
            return new PremiumPackageResponse
            {
                Id = item.PackageId,
                Title = item.Name,
                TargetRole = item.TargetRole,
                PriceVnd = item.Price,
                BillingCycle = item.BillingCycle,
                Features = ReadFeatures(item),
                IsActive = item.IsActive,
                SubscriberCount = subscriberCount,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }
    }
}