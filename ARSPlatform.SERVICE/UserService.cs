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
            var query = _userRepository.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<UserResponse>>(items);

            return new PagedResult<UserResponse>(dtos, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
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

            user.FullName = request.FullName;
            user.AvatarUrl = request.AvatarUrl;
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
