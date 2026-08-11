using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICES
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IProfessionalProfileRepository _professionalProfileRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IWalletRepository walletRepository,
            IProfessionalProfileRepository professionalProfileRepository,
            IMapper mapper,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _walletRepository = walletRepository;
            _professionalProfileRepository = professionalProfileRepository;
            _mapper = mapper;
            _configuration = configuration;
            _emailService = emailService;
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

            // Auto-create ProfessionalProfile for the user
            var professionalProfile = new ProfessionalProfile
            {
                UserId = user.UserId,
                SyncStatus = "pending",
                UpdatedAt = DateTime.UtcNow
            };
            await _professionalProfileRepository.AddAsync(professionalProfile);

            // Auto-create Wallet for the user
            var wallet = new Wallet
            {
                UserId = user.UserId,
                Balance = 0,
                UpdatedAt = DateTime.UtcNow
            };
            await _walletRepository.AddAsync(wallet);

            await _userRepository.SaveChangesAsync();

            // Send registration email confirmation using MailKit
            var verificationToken = GenerateEmailVerificationToken(user.Email);
            try
            {
                var baseVerifyUrl = _configuration["EmailSettings:VerificationUrl"] ?? "https://fe-ars.vercel.app/verify-email";
                var verifyUrl = $"{baseVerifyUrl}?token={Uri.EscapeDataString(verificationToken)}";

                var emailBody = BuildRegisterEmailBody(user.FullName, verifyUrl);
                await _emailService.SendEmailAsync(user.Email, "[ARS] Confirm Your Email Address & Account Registration", emailBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send verification email: {ex.Message}");
            }

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

        private string GenerateEmailVerificationToken(string email)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var keyString = jwtSettings["Key"] ?? "ARSPlatformSuperSecretKeyThatIsAtLeast32BytesLong!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("email", email),
                new Claim("purpose", "email-verification")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"] ?? "ARSPlatformIssuer",
                audience: jwtSettings["Audience"] ?? "ARSPlatformAudience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var keyString = jwtSettings["Key"] ?? "ARSPlatformSuperSecretKeyThatIsAtLeast32BytesLong!";
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "ARSPlatformIssuer",
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"] ?? "ARSPlatformAudience",
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var email = jwtToken.Claims.First(x => x.Type == "email").Value;
                var purpose = jwtToken.Claims.First(x => x.Type == "purpose").Value;

                if (purpose != "email-verification") return false;

                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null) return false;

                user.IsEmailVerified = true;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email verification failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendApprovalEmailAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null) return false;

                var baseVerifyUrl = _configuration["EmailSettings:VerificationUrl"] ?? "https://fe-ars.vercel.app/verify-email";
                var dashboardUrl = baseVerifyUrl.Replace("/verify-email", "/dashboard");

                var emailBody = BuildApprovalEmailBody(user.FullName, dashboardUrl);
                await _emailService.SendEmailAsync(email, "[ARS] Role Verification Approved — Full Access Granted", emailBody);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send approval email: {ex.Message}");
                return false;
            }
        }

        private string BuildRegisterEmailBody(string fullName, string verifyUrl)
        {
            return $@"
<div style=""background-color: #f4f6f9; padding: 40px 0; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; color: #333333;"">
  <div style=""max-width: 550px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);"">
    <div style=""background-color: #243257; padding: 25px; text-align: center;"">
      <svg width=""40"" height=""35"" viewBox=""0 0 40 35"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""vertical-align: middle; margin-right: 10px;"">
        <path d=""M20 2L38 32H2L20 2Z"" stroke=""#00E5FF"" stroke-width=""3"" fill=""none""/>
        <circle cx=""20"" cy=""19"" r=""6"" stroke=""#00E5FF"" stroke-width=""3"" fill=""none""/>
      </svg>
      <span style=""color: #ffffff; font-size: 20px; font-weight: bold; letter-spacing: 1px; vertical-align: middle; font-family: 'Outfit', sans-serif;"">ARS</span>
      <div style=""color: #8fa0c0; font-size: 10px; text-transform: uppercase; letter-spacing: 2px; margin-top: 5px;"">ACADEMIC RESEARCH SHARING</div>
    </div>
    <div style=""padding: 30px 40px;"">
      <h3 style=""margin-top: 0; font-size: 18px; color: #243257;"">Hello {fullName},</h3>
      <p style=""line-height: 1.6; font-size: 14px; color: #555555;"">Thank you for registering on the <strong>Academic Research Sharing (ARS)</strong> platform. Please confirm your email address by clicking the button below to activate your account.</p>
      
      <div style=""text-align: center; margin: 30px 0;"">
        <a href=""{verifyUrl}"" style=""background-color: #007aff; color: #ffffff; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 14px; display: inline-block; box-shadow: 0 4px 6px rgba(0,122,255,0.2);"">Verify Email Address</a>
      </div>
      
      <div style=""background-color: #fff9e6; border: 1px solid #ffe0b2; border-radius: 6px; padding: 15px; margin-top: 25px;"">
        <h4 style=""margin: 0 0 8px 0; color: #e65100; font-size: 14px;"">Account Status Note:</h4>
        <p style=""margin: 0; font-size: 13px; color: #6d4c41; line-height: 1.5;"">Your verification dossier for the Researcher role has been received and is pending Administrator review. In the meantime, you can log in to participate in the ARS Community Forums.</p>
      </div>
    </div>
    <div style=""background-color: #fbfcfd; border-top: 1px solid #f0f2f5; padding: 20px; text-align: center; font-size: 12px; color: #888888;"">
      <p style=""margin: 0 0 10px 0;"">If you did not create an account on ARS, please ignore this email.</p>
      <a href=""#"" style=""color: #007aff; text-decoration: none;"">Privacy Policy</a> | 
      <a href=""#"" style=""color: #007aff; text-decoration: none;"">Terms of Service</a> | 
      <a href=""#"" style=""color: #007aff; text-decoration: none;"">Contact Support</a>
    </div>
  </div>
</div>";
        }

        private string BuildApprovalEmailBody(string fullName, string dashboardUrl)
        {
            return $@"
<div style=""background-color: #f4f6f9; padding: 40px 0; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; color: #333333;"">
  <div style=""max-width: 550px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);"">
    <div style=""background-color: #243257; padding: 25px; text-align: center;"">
      <svg width=""40"" height=""35"" viewBox=""0 0 40 35"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""vertical-align: middle; margin-right: 10px;"">
        <path d=""M20 2L38 32H2L20 2Z"" stroke=""#00E5FF"" stroke-width=""3"" fill=""none""/>
        <circle cx=""20"" cy=""19"" r=""6"" stroke=""#00E5FF"" stroke-width=""3"" fill=""none""/>
      </svg>
      <span style=""color: #ffffff; font-size: 20px; font-weight: bold; letter-spacing: 1px; vertical-align: middle; font-family: 'Outfit', sans-serif;"">ARS</span>
      <div style=""color: #8fa0c0; font-size: 10px; text-transform: uppercase; letter-spacing: 2px; margin-top: 5px;"">ACADEMIC RESEARCH SHARING</div>
    </div>
    <div style=""padding: 30px 40px;"">
      
      <div style=""background-color: #e8f8f5; border: 1px solid #a3e4d7; border-radius: 6px; padding: 12px 15px; margin-bottom: 25px; display: flex; align-items: center;"">
        <div style=""background-color: #16a085; color: #ffffff; width: 20px; height: 20px; border-radius: 50%; text-align: center; line-height: 20px; font-weight: bold; font-size: 12px; margin-right: 10px; display: inline-block;"">✓</div>
        <span style=""color: #0e6251; font-weight: bold; font-size: 14px; font-family: sans-serif;"">Verification Status: <span style=""color: #16a085;"">APPROVED</span></span>
      </div>

      <h3 style=""margin-top: 0; font-size: 18px; color: #243257;"">Hello {fullName},</h3>
      <p style=""line-height: 1.6; font-size: 14px; color: #555555;"">Great news! Your role verification dossier for the <strong>Researcher</strong> role has been officially reviewed and approved by our Administration team.</p>
      
      <div style=""margin-top: 25px;"">
        <h4 style=""margin: 0 0 12px 0; color: #243257; font-size: 14px; font-weight: bold;"">Unlocked Platform Features:</h4>
        <ul style=""list-style: none; padding: 0; margin: 0; font-size: 13px; color: #555555; line-height: 1.8;"">
          <li style=""margin-bottom: 8px; padding-left: 20px; position: relative;"">
            <span style=""color: #16a085; font-weight: bold; position: absolute; left: 0;"">✓</span> Full manuscript upload and PDF paper management
          </li>
          <li style=""margin-bottom: 8px; padding-left: 20px; position: relative;"">
            <span style=""color: #16a085; font-weight: bold; position: absolute; left: 0;"">✓</span> Access to Find Reviewers directory and peer review requests
          </li>
          <li style=""margin-bottom: 8px; padding-left: 20px; position: relative;"">
            <span style=""color: #16a085; font-weight: bold; position: absolute; left: 0;"">✓</span> Integrated Escrow Wallet and transaction processing
          </li>
          <li style=""margin-bottom: 8px; padding-left: 20px; position: relative;"">
            <span style=""color: #16a085; font-weight: bold; position: absolute; left: 0;"">✓</span> Direct messaging and academic collaboration tools
          </li>
        </ul>
      </div>

      <div style=""text-align: center; margin: 30px 0 10px 0;"">
        <a href=""{dashboardUrl}"" style=""background-color: #007aff; color: #ffffff; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 14px; display: inline-block; box-shadow: 0 4px 6px rgba(0,122,255,0.2);"">Go to ARS Dashboard</a>
      </div>
    </div>
    <div style=""background-color: #fbfcfd; border-top: 1px solid #f0f2f5; padding: 25px; text-align: center; font-size: 11px; color: #888888; line-height: 1.5;"">
      <p style=""margin: 0 0 10px 0;"">Academic Research Sharing Platform &middot; Ho Chi Minh City, Vietnam</p>
      <a href=""#"" style=""color: #007aff; text-decoration: none;"">Help Center</a> | 
      <a href=""#"" style=""color: #007aff; text-decoration: none;"">Account Settings</a>
    </div>
  </div>
</div>";
        }
    }
}
