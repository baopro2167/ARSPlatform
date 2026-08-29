using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class AnnualFeeService : IAnnualFeeService
    {
        private readonly IMembershipPackageRepository _packageRepository;
        private readonly IMembershipPurchaseRepository _purchaseRepository;

        private static readonly HashSet<string> SupportedRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Researcher",
            "Reviewer",
            "Lecturer",
            "Graduate Student",
            "Student",
            "Admin",
            "Guest"
        };

        private static readonly HashSet<string> SupportedBillingCycles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Annual",
            "Monthly",
            "Quarterly",
            "SemiAnnual"
        };

        public AnnualFeeService(
            IMembershipPackageRepository packageRepository,
            IMembershipPurchaseRepository purchaseRepository)
        {
            _packageRepository = packageRepository;
            _purchaseRepository = purchaseRepository;
        }

        public async Task<IEnumerable<AnnualFeeResponse>> GetAllAsync(bool? isActive = null, string? targetRole = null, string? billingCycle = null)
        {
            var packages = await _packageRepository.GetAllAsync();

            var query = packages.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(targetRole))
            {
                query = query.Where(p => string.Equals(p.TargetRole, targetRole, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(billingCycle))
            {
                query = query.Where(p => string.Equals(p.BillingCycle, billingCycle, StringComparison.OrdinalIgnoreCase));
            }

            return query
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.TargetRole)
                .ThenBy(p => p.Price)
                .Select(MapToResponse)
                .ToList();
        }

        public async Task<AnnualFeeResponse?> GetByIdAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            return package == null ? null : MapToResponse(package);
        }

        public async Task<AnnualFeeResponse> CreateAsync(AnnualFeeCreateRequest request)
        {
            ValidateRequest(request.TargetRole, request.PriceVnd, request.BillingCycle);

            var normalizedRole = NormalizeRole(request.TargetRole);
            var normalizedCycle = NormalizeCycle(request.BillingCycle);

            // Kiểm tra ràng buộc Unique active tier per role/cycle
            if (request.IsActive)
            {
                var existingActive = (await _packageRepository.GetAllAsync(p =>
                    p.IsActive &&
                    p.TargetRole.ToLower() == normalizedRole.ToLower() &&
                    p.BillingCycle.ToLower() == normalizedCycle.ToLower())).FirstOrDefault();

                if (existingActive != null)
                {
                    throw new InvalidOperationException($"An active annual fee tier for role '{normalizedRole}' with billing cycle '{normalizedCycle}' already exists (ID: {existingActive.PackageId}, Title: '{existingActive.Name}').");
                }
            }

            var durationDays = CalculateDurationDays(normalizedCycle);

            var package = new MembershipPackage
            {
                Name = request.Title.Trim(),
                Price = request.PriceVnd,
                TargetRole = normalizedRole,
                BillingCycle = normalizedCycle,
                DurationDays = durationDays,
                Features = SerializeFeatures(request.Features),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _packageRepository.AddAsync(package);
            await _packageRepository.SaveChangesAsync();

            return MapToResponse(package);
        }

        public async Task<AnnualFeeResponse?> UpdateAsync(int id, AnnualFeeUpdateRequest request)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null) return null;

            ValidateRequest(request.TargetRole, request.PriceVnd, request.BillingCycle);

            var normalizedRole = NormalizeRole(request.TargetRole);
            var normalizedCycle = NormalizeCycle(request.BillingCycle);

            // Kiểm tra ràng buộc Unique active tier per role/cycle (trừ chính bản ghi này)
            if (request.IsActive)
            {
                var existingActive = (await _packageRepository.GetAllAsync(p =>
                    p.PackageId != id &&
                    p.IsActive &&
                    p.TargetRole.ToLower() == normalizedRole.ToLower() &&
                    p.BillingCycle.ToLower() == normalizedCycle.ToLower())).FirstOrDefault();

                if (existingActive != null)
                {
                    throw new InvalidOperationException($"An active annual fee tier for role '{normalizedRole}' with billing cycle '{normalizedCycle}' already exists (ID: {existingActive.PackageId}, Title: '{existingActive.Name}').");
                }
            }

            var durationDays = CalculateDurationDays(normalizedCycle);

            package.Name = request.Title.Trim();
            package.Price = request.PriceVnd;
            package.TargetRole = normalizedRole;
            package.BillingCycle = normalizedCycle;
            package.DurationDays = durationDays;
            package.Features = SerializeFeatures(request.Features);
            package.IsActive = request.IsActive;
            package.UpdatedAt = DateTime.UtcNow;

            _packageRepository.Update(package);
            await _packageRepository.SaveChangesAsync();

            return MapToResponse(package);
        }

        public async Task<AnnualFeeResponse?> ToggleActiveAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null) return null;

            var targetActiveState = !package.IsActive;

            if (targetActiveState)
            {
                var existingActive = (await _packageRepository.GetAllAsync(p =>
                    p.PackageId != id &&
                    p.IsActive &&
                    p.TargetRole.ToLower() == package.TargetRole.ToLower() &&
                    p.BillingCycle.ToLower() == package.BillingCycle.ToLower())).FirstOrDefault();

                if (existingActive != null)
                {
                    throw new InvalidOperationException($"Cannot activate this fee. An active annual fee tier for role '{package.TargetRole}' with billing cycle '{package.BillingCycle}' already exists (ID: {existingActive.PackageId}, Title: '{existingActive.Name}').");
                }
            }

            package.IsActive = targetActiveState;
            package.UpdatedAt = DateTime.UtcNow;

            _packageRepository.Update(package);
            await _packageRepository.SaveChangesAsync();

            return MapToResponse(package);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null) return false;

            // Kiểm tra nếu có lịch sử giao dịch mua gói
            var hasPurchases = await _purchaseRepository.ExistsAsync(p => p.PackageId == id);
            if (hasPurchases)
            {
                // Deactivate để bảo toàn dữ liệu lịch sử
                package.IsActive = false;
                package.UpdatedAt = DateTime.UtcNow;
                _packageRepository.Update(package);
                await _packageRepository.SaveChangesAsync();
                return true;
            }

            _packageRepository.Delete(package);
            await _packageRepository.SaveChangesAsync();
            return true;
        }

        private static void ValidateRequest(string targetRole, decimal priceVnd, string billingCycle)
        {
            if (string.IsNullOrWhiteSpace(targetRole))
            {
                throw new ArgumentException("Target role is required.");
            }

            if (!SupportedRoles.Contains(targetRole.Trim()))
            {
                throw new ArgumentException($"Unsupported target role: '{targetRole}'. Supported roles are: {string.Join(", ", SupportedRoles)}.");
            }

            if (priceVnd <= 0)
            {
                throw new ArgumentException("Price VND must be a positive amount greater than 0.");
            }

            if (string.IsNullOrWhiteSpace(billingCycle))
            {
                throw new ArgumentException("Billing cycle is required.");
            }

            if (!SupportedBillingCycles.Contains(billingCycle.Trim()))
            {
                throw new ArgumentException($"Unsupported billing cycle: '{billingCycle}'. Supported cycles are: {string.Join(", ", SupportedBillingCycles)}.");
            }
        }

        private static string NormalizeRole(string role)
        {
            var match = SupportedRoles.FirstOrDefault(r => string.Equals(r, role.Trim(), StringComparison.OrdinalIgnoreCase));
            return match ?? role.Trim();
        }

        private static string NormalizeCycle(string cycle)
        {
            var match = SupportedBillingCycles.FirstOrDefault(c => string.Equals(c, cycle.Trim(), StringComparison.OrdinalIgnoreCase));
            return match ?? cycle.Trim();
        }

        private static int CalculateDurationDays(string billingCycle)
        {
            return billingCycle.ToLowerInvariant() switch
            {
                "monthly" => 30,
                "quarterly" => 90,
                "semiannual" => 180,
                "annual" => 365,
                _ => 365
            };
        }

        private static string SerializeFeatures(List<string>? features)
        {
            if (features == null || !features.Any())
            {
                return JsonSerializer.Serialize(new List<string>());
            }
            var cleanList = features
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f.Trim())
                .ToList();
            return JsonSerializer.Serialize(cleanList);
        }

        private static AnnualFeeResponse MapToResponse(MembershipPackage package)
        {
            List<string> features = new();
            if (!string.IsNullOrWhiteSpace(package.Features))
            {
                try
                {
                    features = JsonSerializer.Deserialize<List<string>>(package.Features) ?? new();
                }
                catch
                {
                    features = package.Features
                        .Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
                }
            }

            return new AnnualFeeResponse
            {
                Id = package.PackageId,
                TargetRole = package.TargetRole ?? "Researcher",
                Title = package.Name,
                PriceVnd = package.Price,
                BillingCycle = package.BillingCycle ?? "Annual",
                Features = features,
                IsActive = package.IsActive,
                UpdatedAt = package.UpdatedAt
            };
        }
    }
}
