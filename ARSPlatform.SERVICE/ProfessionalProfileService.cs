using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class ProfessionalProfileService : IProfessionalProfileService
    {
        private readonly IProfessionalProfileRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public ProfessionalProfileService(
            IProfessionalProfileRepository repository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _repository = repository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProfessionalProfileResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllWithUserAndFieldAsync();
            return _mapper.Map<IEnumerable<ProfessionalProfileResponse>>(items);
        }

        public async Task<PagedResult<ProfessionalProfileResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(
                paginationParams,
                includes: new System.Linq.Expressions.Expression<System.Func<ProfessionalProfile, object>>[]
                {
                    x => x.User!,
                    x => x.SubField!
                });

            var dtos = _mapper.Map<List<ProfessionalProfileResponse>>(paged.Items);
            return new PagedResult<ProfessionalProfileResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ProfessionalProfileResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ProfessionalProfileResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdWithUserAndFieldAsync(id);
            if (item != null)
            {
                return _mapper.Map<ProfessionalProfileResponse>(item);
            }

            // Fallback: If user exists in User table but hasn't created a ProfessionalProfile row yet
            var user = await _userRepository.GetWithRoleByIdAsync(id);
            if (user != null)
            {
                return new ProfessionalProfileResponse
                {
                    UserId = user.UserId,
                    FullName = user.FullName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    AvatarUrl = user.AvatarUrl,
                    OrcidId = user.OrcidId,
                    IsOrcidVerified = user.IsOrcidVerified,
                    OrcidVerifiedAt = user.OrcidVerifiedAt,
                    IsAvailable = user.IsAvailableForReview
                };
            }

            return null;
        }

        public async Task<ProfessionalProfileResponse> CreateAsync(ProfessionalProfileCreateRequest request)
        {
            var item = _mapper.Map<ProfessionalProfile>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithUserAndFieldAsync(item.UserId);
            return _mapper.Map<ProfessionalProfileResponse>(created);
        }

        public async Task<ProfessionalProfileResponse?> UpdateAsync(int id, ProfessionalProfileUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                // If user exists, create on update (upsert)
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null) return null;

                var newProf = _mapper.Map<ProfessionalProfile>(request);
                newProf.UserId = id;
                await _repository.AddAsync(newProf);
                await _repository.SaveChangesAsync();

                var created = await _repository.GetByIdWithUserAndFieldAsync(id);
                return _mapper.Map<ProfessionalProfileResponse>(created);
            }

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithUserAndFieldAsync(id);
            return _mapper.Map<ProfessionalProfileResponse>(updated);
        }

        public async Task<ProfessionalProfileResponse?> UpdateAvailabilityAsync(int id, bool isAvailable)
        {
            var item = await _repository.UpdateAvailabilityAsync(id, isAvailable);
            if (item == null) return null;

            await _repository.SaveChangesAsync();
            return _mapper.Map<ProfessionalProfileResponse>(item);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return false;

            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
