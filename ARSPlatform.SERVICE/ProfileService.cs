using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using ProfileEntity = ARSPlatform.MODEL.Entities.Profile;

namespace ARSPlatform.SERVICES
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public ProfileService(
            IProfileRepository repository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _repository = repository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProfileResponse>> GetAllAsync()
        {
            var items = await _repository.GetAllWithUserAsync();
            return _mapper.Map<IEnumerable<ProfileResponse>>(items);
        }

        public async Task<PagedResult<ProfileResponse>> GetPagedAsync(PaginationParams paginationParams)
        {
            var paged = await _repository.GetPagedAsync(paginationParams, includes: x => x.User!);
            var dtos = _mapper.Map<List<ProfileResponse>>(paged.Items);
            return new PagedResult<ProfileResponse>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task<PagedResult<ProfileResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<ProfileResponse?> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdWithUserAsync(id);
            if (item != null)
            {
                return _mapper.Map<ProfileResponse>(item);
            }

            // Fallback: If user exists in User table but hasn't created a Profile row yet
            var user = await _userRepository.GetWithRoleByIdAsync(id);
            if (user != null)
            {
                var roleName = user.UserRoles?.FirstOrDefault()?.Role?.Name 
                            ?? user.UserRoles?.FirstOrDefault()?.UserRole1 
                            ?? string.Empty;

                return new ProfileResponse
                {
                    UserId = user.UserId,
                    FullName = user.FullName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    AvatarUrl = user.AvatarUrl,
                    RoleName = roleName,
                    AcademicTitle = string.Empty,
                    Institution = string.Empty,
                    Bio = string.Empty,
                    Keywords = Array.Empty<string>(),
                    OrcidId = user.OrcidId,
                    IsOrcidVerified = user.IsOrcidVerified,
                    OrcidVerifiedAt = user.OrcidVerifiedAt
                };
            }

            return null;
        }

        public async Task<ProfileResponse> CreateAsync(ProfileCreateRequest request)
        {
            var existing = await _repository.GetByIdAsync(request.UserId);
            if (existing != null)
            {
                throw new InvalidOperationException("A profile already exists for this user.");
            }

            var item = _mapper.Map<ProfileEntity>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithUserAsync(item.UserId);
            return _mapper.Map<ProfileResponse>(created);
        }

        public async Task<ProfileResponse?> UpdateAsync(int id, ProfileUpdateRequest request)
        {
            if (request.UserId != id)
            {
                throw new ArgumentException("The request UserId must match the route id.");
            }

            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                // If user exists, create profile row on update (upsert)
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null) return null;

                var newProfile = _mapper.Map<ProfileEntity>(request);
                newProfile.UserId = id;
                await _repository.AddAsync(newProfile);
                await _repository.SaveChangesAsync();

                var created = await _repository.GetByIdWithUserAsync(id);
                return _mapper.Map<ProfileResponse>(created);
            }

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithUserAsync(id);
            return _mapper.Map<ProfileResponse>(updated);
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
