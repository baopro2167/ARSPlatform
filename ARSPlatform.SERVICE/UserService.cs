using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICES
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IExternalApiService _externalApiService;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IExternalApiService externalApiService, IMapper mapper)
        {
            _userRepository = userRepository;
            _externalApiService = externalApiService;
            _mapper = mapper;
        }

        public async Task<PagedResult<UserResponse>> GetUsersAsync(PaginationParams paginationParams)
        {
            return await GetUsersAsync(paginationParams, role: null, isActive: null, excludeUserId: null);
        }

        public async Task<PagedResult<UserResponse>> GetUsersAsync(
            PaginationParams paginationParams,
            string? role = null,
            bool? isActive = null,
            int? excludeUserId = null)
        {
            var query = _userRepository.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsNoTracking();

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(role) && !role.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                var roleTrimmed = role.Trim();
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == roleTrimmed));
            }

            if (excludeUserId.HasValue && excludeUserId.Value > 0)
            {
                query = query.Where(u => u.UserId != excludeUserId.Value);
            }

            var totalCount = await query.CountAsync();

            var pageNumber = paginationParams.PageNumber < 1 ? 1 : paginationParams.PageNumber;
            var pageSize = paginationParams.PageSize < 1 ? 10 : paginationParams.PageSize;

            var items = await query
                .OrderBy(u => u.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<UserResponse>>(items);

            return new PagedResult<UserResponse>(dtos, totalCount, pageNumber, pageSize);
        }

        public async Task<List<UserResponse>> GetLecturersRosterAsync(int? excludeUserId = null)
        {
            var paged = await GetUsersAsync(
                new PaginationParams { PageNumber = 1, PageSize = 1000 },
                role: "Lecturer",
                isActive: true,
                excludeUserId: excludeUserId);
            return paged.Items;
        }

        public async Task<PagedResult<UserResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await GetUsersAsync(new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<UserResponse?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetWithRoleByIdAsync(id);
            return user != null ? _mapper.Map<UserResponse>(user) : null;
        }

        public async Task<UserResponse?> UpdateUserAsync(int id, UserUpdateRequest request)
        {
            var user = await _userRepository.GetWithRoleByIdAsync(id);
            if (user == null)
                return null;

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (request.AvatarUrl != null)
                user.AvatarUrl = request.AvatarUrl;

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return _mapper.Map<UserResponse>(user);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return false;

            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }
    }
}
