using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE;
using System.Net.Http;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using AutoMapper;
using Google.Apis.Auth;
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
        private readonly IRoleRequestRepository _roleRequestRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IProfessionalProfileRepository _professionalProfileRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRoleRequestRepository roleRequestRepository,
            IWalletRepository walletRepository,
            IProfessionalProfileRepository professionalProfileRepository,
            IMapper mapper,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _roleRequestRepository = roleRequestRepository;
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

            var requestableRoles = new[]
            {
                "Researcher",
                "Reviewer",
                "Lecturer",
                "Graduate Student"
            };

            var requestedRoleName = requestableRoles.FirstOrDefault(role =>
                string.Equals(
                    role,
                    request.Role.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (requestedRoleName == null)
                throw new Exception("Requested role is not allowed for self-registration.");

            var requestedRole = await _roleRepository.GetByNameAsync(requestedRoleName);
            if (requestedRole == null)
                throw new Exception($"Requested role '{requestedRoleName}' is not configured in the database.");

            var now = DateTime.UtcNow;

            var user = _mapper.Map<User>(request);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.IsActive = false;
            user.IsEmailVerified = false;
            user.VerificationStatus = "Pending";
            user.ProofDocumentUrl = request.PdfUrl.Trim();
            user.CreatedAt = now;
            user.UpdatedAt = now;

            // Pending accounts have no approved business role in UserRole.
            // "Guest" is only an effective JWT role for read-only Forum access.
            user.UserRoles = new List<UserRole>();

            await _userRepository.AddAsync(user);

            // Auto-create ProfessionalProfile for the user
            var professionalProfile = new ProfessionalProfile
            {
                User = user,
                SyncStatus = "pending",
                UpdatedAt = now
            };
            await _professionalProfileRepository.AddAsync(professionalProfile);

            // Auto-create Wallet for the user
            var wallet = new Wallet
            {
                User = user,
                Balance = 0,
                UpdatedAt = now
            };
            await _walletRepository.AddAsync(wallet);

            // Create pending role request for Admin review.
            // The requested role is not inserted into UserRole until Admin approval.
            var roleRequest = new RoleRequest
            {
                User = user,
                RequestedRoleId = requestedRole.RoleId,
                PhoneNumber = request.PhoneNumber.Trim(),
                ProofDocumentUrl = request.PdfUrl.Trim(),
                Status = "PENDING",
                RequestType = "INITIAL_REGISTRATION",
                CreatedAt = now,
                UpdatedAt = now
            };
            await _roleRequestRepository.AddAsync(roleRequest);

            // All registration entities share the same scoped AppDbContext.
            // A single SaveChanges call persists the registration atomically.
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

            var token = GenerateJwtToken(createdUser, "Guest");

            return new AuthResponse
            {
                UserId = createdUser.UserId,
                Token = token,
                Username = createdUser.FullName,
                Email = createdUser.Email,
                Role = "Guest",
                IsEmailVerified = createdUser.IsEmailVerified,
                IsActive = createdUser.IsActive,
                VerificationStatus = createdUser.VerificationStatus
            };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Email);
            if (user == null)
                return null;

            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            if (string.Equals(user.VerificationStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                return null;

            if (string.Equals(user.VerificationStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                var guestToken = GenerateJwtToken(user, "Guest");

                return new AuthResponse
                {
                    UserId = user.UserId,
                    Token = guestToken,
                    Username = user.FullName,
                    Email = user.Email,
                    Role = "Guest",
                    IsEmailVerified = user.IsEmailVerified,
                    IsActive = user.IsActive,
                    VerificationStatus = user.VerificationStatus
                };
            }

            if (user.IsActive == false)
                return null;

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                UserId = user.UserId,
                Token = token,
                Username = user.FullName,
                Email = user.Email,
                Role = user.UserRoles.FirstOrDefault()?.Role?.Name,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                VerificationStatus = user.VerificationStatus
            };
        }

        public async Task<AuthResponse?> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var googleSettings = _configuration.GetSection("GoogleAuth");
            var clientId = googleSettings["ClientId"];

            if (string.IsNullOrEmpty(clientId))
                throw new Exception("Google ClientId is not configured.");

            GoogleJsonWebSignature.Payload? payload;
            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, validationSettings);
            }
            catch (Exception)
            {
                return null;
            }

            var user = await _userRepository.GetByUsernameAsync(payload.Email);
            string effectiveRole;

            if (user == null)
            {
                return new AuthResponse
                {
                    Email = payload.Email,
                    Username = payload.Name ?? payload.Email.Split('@')[0],
                    IsNewUser = true,
                    VerificationStatus = "Pending"
                };
            }
            else
            {
                if (user.IsActive == false && !string.Equals(user.VerificationStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (string.Equals(user.VerificationStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (string.Equals(user.VerificationStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    var guestToken = GenerateJwtToken(user, "Guest");
                    return new AuthResponse
                    {
                        UserId = user.UserId,
                        Token = guestToken,
                        Username = user.FullName,
                        Email = user.Email,
                        Role = "Guest",
                        IsEmailVerified = user.IsEmailVerified,
                        IsActive = user.IsActive,
                        VerificationStatus = user.VerificationStatus,
                        IsNewUser = false
                    };
                }

                effectiveRole = user.UserRoles.FirstOrDefault()?.Role?.Name ?? "Guest";
            }

            var token = GenerateJwtToken(user, effectiveRole);
            var rolesList = user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).ToList();

            return new AuthResponse
            {
                UserId = user.UserId,
                Token = token,
                Username = user.FullName,
                Email = user.Email,
                Role = effectiveRole,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                VerificationStatus = user.VerificationStatus,
                IsNewUser = false,
                Roles = rolesList
            };
        }

        private string GenerateJwtToken(User user, string? effectiveRole = null)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var keyString = jwtSettings["Key"] ?? "ARSPlatformSuperSecretKeyThatIsAtLeast32BytesLong!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var roleName = effectiveRole ?? user.UserRoles.FirstOrDefault()?.Role?.Name;
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

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
      <p style=""line-height: 1.6; font-size: 14px; color: #555555;"">Thank you for registering on the <strong>Academic Research Sharing (ARS)</strong> platform. Please confirm your email address by clicking the button below. Your account will remain pending until Administrator approval is completed.</p>
      
      <div style=""text-align: center; margin: 30px 0;"">
        <a href=""{verifyUrl}"" style=""background-color: #007aff; color: #ffffff; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 14px; display: inline-block; box-shadow: 0 4px 6px rgba(0,122,255,0.2);"">Verify Email Address</a>
      </div>
      
      <div style=""background-color: #fff9e6; border: 1px solid #ffe0b2; border-radius: 6px; padding: 15px; margin-top: 25px;"">
        <h4 style=""margin: 0 0 8px 0; color: #e65100; font-size: 14px;"">Account Status Note:</h4>
        <p style=""margin: 0; font-size: 13px; color: #6d4c41; line-height: 1.5;"">Your account is pending Administrator verification. Until approval, your account has read-only access to public ARS Community Forum posts.</p>
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

        public string GetGoogleAuthorizationUrl(string redirectUri, string scopes)
        {
            var isGoogleMeet = scopes.Contains("meetings.space.created");
            var clientId = isGoogleMeet 
                ? (Environment.GetEnvironmentVariable("GoogleMeetSettings__ClientId") ?? _configuration["GoogleMeetSettings:ClientId"])
                : (Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? _configuration["GoogleAuth:ClientId"]);

            if (string.IsNullOrEmpty(clientId))
                throw new Exception("Google Client ID is not configured.");

            var accessType = isGoogleMeet ? "&access_type=offline&prompt=consent" : "";
            
            return $"https://accounts.google.com/o/oauth2/v2/auth?" +
                   $"client_id={Uri.EscapeDataString(clientId)}" +
                   $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                   $"&response_type=code" +
                   $"&scope={Uri.EscapeDataString(scopes)}" +
                   accessType;
        }

        public async Task<string?> ExchangeCodeForRefreshTokenAsync(string code, string redirectUri)
        {
            var clientId = Environment.GetEnvironmentVariable("GoogleMeetSettings__ClientId") 
                ?? _configuration["GoogleMeetSettings:ClientId"] 
                ?? "";
            var clientSecret = Environment.GetEnvironmentVariable("GoogleMeetSettings__ClientSecret") 
                ?? _configuration["GoogleMeetSettings:ClientSecret"] 
                ?? "";

            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code"
                })
            };

            using var response = await httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to exchange code: {responseBody}");
            }

            using var document = System.Text.Json.JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("refresh_token", out var refreshTokenElement))
            {
                return refreshTokenElement.GetString();
            }

            return $"Error: refresh_token not found in response. Make sure you selected prompt=consent and have not already authorized this client. Response: {responseBody}";
        }

        public async Task<AuthResponse?> AuthenticateGoogleLoginAsync(string code, string redirectUri)
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
                ?? _configuration["GoogleAuth:ClientId"] 
                ?? "";
            var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") 
                ?? _configuration["GoogleAuth:ClientSecret"] 
                ?? "";

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                throw new Exception("Google login credentials are not configured on the backend.");

            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code"
                })
            };

            using var response = await httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to exchange Google OAuth code: {responseBody}");
            }

            using var document = System.Text.Json.JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("id_token", out var idTokenElement))
            {
                throw new Exception("Google OAuth response did not contain an id_token.");
            }

            var idToken = idTokenElement.GetString();
            if (string.IsNullOrEmpty(idToken))
            {
                throw new Exception("Empty id_token returned from Google.");
            }

            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            var requestDto = new GoogleLoginRequest { Credential = idToken };
            return await GoogleLoginAsync(requestDto);
        }

        public async Task<AuthResponse?> CompleteGoogleRegistrationAsync(int userId, CompleteGoogleRegistrationRequest request)
        {
            var user = await _userRepository.GetWithRoleByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var existingRequests = await _roleRequestRepository.ExistsAsync(rr => 
                rr.User.UserId == userId && 
                (rr.Status == "PENDING" || rr.Status == "APPROVED"));
            
            if (existingRequests)
            {
                throw new Exception("A role request already exists or has already been approved.");
            }

            var requestableRoles = new[]
            {
                "Researcher",
                "Reviewer",
                "Lecturer",
                "Graduate Student"
            };

            var requestedRoleName = requestableRoles.FirstOrDefault(role =>
                string.Equals(
                    role,
                    request.Role.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (requestedRoleName == null)
                throw new Exception("Requested role is not allowed.");

            var requestedRole = await _roleRepository.GetByNameAsync(requestedRoleName);
            if (requestedRole == null)
                throw new Exception($"Requested role '{requestedRoleName}' is not configured in the database.");

            var now = DateTime.UtcNow;

            user.ProofDocumentUrl = request.PdfUrl.Trim();
            user.VerificationStatus = "Pending";
            user.IsActive = false;
            user.UpdatedAt = now;

            var roleRequest = new RoleRequest
            {
                User = user,
                RequestedRoleId = requestedRole.RoleId,
                PhoneNumber = request.PhoneNumber.Trim(),
                ProofDocumentUrl = request.PdfUrl.Trim(),
                Status = "PENDING",
                RequestType = "INITIAL_REGISTRATION",
                CreatedAt = now,
                UpdatedAt = now
            };
            await _roleRequestRepository.AddAsync(roleRequest);

            await _userRepository.SaveChangesAsync();

            var token = GenerateJwtToken(user, "Guest");

            return new AuthResponse
            {
                UserId = user.UserId,
                Token = token,
                Username = user.FullName,
                FullName = user.FullName,
                Email = user.Email,
                Role = null,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                VerificationStatus = user.VerificationStatus,
                IsNewUser = false,
                RequiresOnboarding = false,
                EffectiveRole = "Guest",
                Roles = new List<string>()
            };
        }

        public async Task<AuthResponse?> SelectRoleAsync(int userId, string roleName)
        {
            var user = await _userRepository.GetWithRoleByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            if (user.IsActive == false)
            {
                throw new Exception("User is inactive.");
            }

            var hasRole = user.UserRoles.Any(ur => 
                string.Equals(ur.Role?.Name, roleName, StringComparison.OrdinalIgnoreCase));

            if (!hasRole)
            {
                throw new Exception($"Role '{roleName}' is not assigned to this user.");
            }

            var chosenRole = user.UserRoles.First(ur => 
                string.Equals(ur.Role?.Name, roleName, StringComparison.OrdinalIgnoreCase)).Role!.Name;

            var token = GenerateJwtToken(user, chosenRole);
            var rolesList = user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).ToList();

            return new AuthResponse
            {
                UserId = user.UserId,
                Token = token,
                Username = user.FullName,
                FullName = user.FullName,
                Email = user.Email,
                Role = chosenRole,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                VerificationStatus = user.VerificationStatus,
                IsNewUser = false,
                RequiresOnboarding = false,
                EffectiveRole = chosenRole,
                Roles = rolesList
            };
        }
    }
}