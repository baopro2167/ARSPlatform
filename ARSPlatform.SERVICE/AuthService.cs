using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICES
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IMapper mapper,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.ExistsAsync(u => u.Email == request.Email))
                throw new Exception("Email is already registered.");

            var user = _mapper.Map<User>(request);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.IsActive = true;
            user.IsEmailVerified = false;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            var defaultRole = await _roleRepository.GetByNameAsync("Researcher");
            if (defaultRole == null)
            {
                defaultRole = new Role { Name = "Researcher", CreatedAt = DateTime.UtcNow };
                await _roleRepository.AddAsync(defaultRole);
                await _roleRepository.SaveChangesAsync();
            }
            user.UserRoles = new List<UserRole> { new UserRole { RoleId = defaultRole.RoleId, CreatedAt = DateTime.UtcNow } };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var createdUser = await _userRepository.GetWithRoleByIdAsync(user.UserId);
            if (createdUser == null) return null;

            var token = GenerateJwtToken(createdUser);

            return new AuthResponse
            {
                UserId = createdUser.UserId,
                Token = token,
                Username = createdUser.FullName,
                Email = createdUser.Email,
                Role = createdUser.UserRoles.FirstOrDefault()?.Role?.Name ?? "Researcher"
            };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Email);
            if (user == null)
                return null;

            if (user.IsActive == false)
                return null;

            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                UserId = user.UserId,
                Token = token,
                Username = user.FullName,
                Email = user.Email,
                Role = user.UserRoles.FirstOrDefault()?.Role?.Name ?? "Researcher"
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var keyString = jwtSettings["Key"] ?? "ARSPlatformSuperSecretKeyThatIsAtLeast32BytesLong!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserRoles.FirstOrDefault()?.Role?.Name ?? "Researcher")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"] ?? "ARSPlatformIssuer",
                audience: jwtSettings["Audience"] ?? "ARSPlatformAudience",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings["DurationInDays"] ?? "7")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
