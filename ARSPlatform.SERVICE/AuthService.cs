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
            if (await _userRepository.ExistsAsync(u => u.Username == request.Username))
                throw new Exception("Username is already taken.");

            if (await _userRepository.ExistsAsync(u => u.Email == request.Email))
                throw new Exception("Email is already registered.");

            var user = _mapper.Map<User>(request);
            user.Id = Guid.NewGuid();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var defaultRole = await _roleRepository.GetByNameAsync("Researcher");
            user.RoleId = defaultRole != null ? defaultRole.Id : 2;

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var createdUser = await _userRepository.GetWithRoleByIdAsync(user.Id);
            if (createdUser == null) return null;

            var token = GenerateJwtToken(createdUser);

            return new AuthResponse
            {
                Token = token,
                Username = createdUser.Username,
                Email = createdUser.Email,
                Role = createdUser.Role.Name
            };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.Name
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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "Researcher")
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
