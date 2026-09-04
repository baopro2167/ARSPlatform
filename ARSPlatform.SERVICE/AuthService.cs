using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE;
using System.Net.Http;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using ARSPlatform.SERVICE.ExternalServices;
using AutoMapper;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
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
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IOrcidLinkSessionRepository _orcidLinkSessionRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRoleRequestRepository roleRequestRepository,
            IWalletRepository walletRepository,
            IProfessionalProfileRepository professionalProfileRepository,
            IUserRoleRepository userRoleRepository,
            IOrcidLinkSessionRepository orcidLinkSessionRepository,
            IMapper mapper,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _roleRequestRepository = roleRequestRepository;
            _walletRepository = walletRepository;
            _professionalProfileRepository = professionalProfileRepository;
            _userRoleRepository = userRoleRepository;
            _orcidLinkSessionRepository = orcidLinkSessionRepository;
            _mapper = mapper;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.IsEmailVerified == true)
            {
                throw new Exception("Email is already registered and verified. Please proceed to login.");
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
                throw new Exception("Requested role is not allowed for self-registration.");

            var requestedRole = await _roleRepository.GetByNameAsync(requestedRoleName);
            if (requestedRole == null)
                throw new Exception($"Requested role '{requestedRoleName}' is not configured in the database.");

            var now = DateTime.UtcNow;

            /*
                ORCID is optional.

                If OrcidTicket is not supplied, registration continues
                exactly like a normal ARS registration.

                If OrcidTicket is supplied, it must come from a successful
                REGISTRATION ORCID OAuth session.
            */
            OrcidLinkSession? orcidRegistrationSession = null;
            string? verifiedOrcidId = null;
            string? verifiedOrcidDisplayName = null;

            if (!string.IsNullOrWhiteSpace(request.OrcidTicket))
            {
                var rawTicket = request.OrcidTicket.Trim();

                var ticketHash =
                    ComputeSha256(rawTicket);

                orcidRegistrationSession =
                    await _orcidLinkSessionRepository
                        .GetByTicketHashAsync(ticketHash);

                if (orcidRegistrationSession == null)
                {
                    throw new Exception(
                        "Invalid ORCID registration ticket.");
                }

                /*
                    A registration ticket can only come from
                    REGISTRATION context.

                    ACCOUNT_LINK sessions must never be accepted here.
                */
                if (!string.Equals(
                        orcidRegistrationSession.Context,
                        "REGISTRATION",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        "The ORCID ticket is not valid for registration.");
                }

                /*
                    AUTHENTICATED means:
                    ORCID OAuth succeeded, but the ticket has not yet
                    been consumed by account registration.
                */
                if (!string.Equals(
                        orcidRegistrationSession.Status,
                        "AUTHENTICATED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(
                            orcidRegistrationSession.Status,
                            "COMPLETED",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception(
                            "This ORCID registration ticket has already been used.");
                    }

                    throw new Exception(
                        "The ORCID registration ticket is not active.");
                }

                if (orcidRegistrationSession.ExpiresAt <= now)
                {
                    throw new Exception(
                        "The ORCID registration ticket has expired. Please connect ORCID again.");
                }

                if (string.IsNullOrWhiteSpace(
                        orcidRegistrationSession.AuthenticatedOrcidId))
                {
                    throw new Exception(
                        "The ORCID registration session does not contain an authenticated ORCID iD.");
                }

                /*
                    Never trust even the value stored in the temporary
                    OAuth session without validating its ORCID format
                    again at the User boundary.
                */
                if (!OrcidIdUtility.TryNormalizeAndValidate(
                        orcidRegistrationSession.AuthenticatedOrcidId,
                        out verifiedOrcidId))
                {
                    throw new Exception(
                        "The authenticated ORCID iD is invalid.");
                }

                verifiedOrcidDisplayName =
                    string.IsNullOrWhiteSpace(
                        orcidRegistrationSession.DisplayName)
                        ? null
                        : orcidRegistrationSession.DisplayName.Trim();

                /*
                    One ORCID must belong to only one ARS User.

                    GetByOrcidAsync was added in Step 4D.
                */
                var existingOrcidUser =
                    await _userRepository
                        .GetByOrcidAsync(verifiedOrcidId);

                if (existingOrcidUser != null &&
                    (existingUser == null ||
                     existingOrcidUser.UserId != existingUser.UserId))
                {
                    throw new Exception(
                        "This ORCID iD is already connected to another ARS account.");
                }

                /*
                    If this email already has an unfinished/unverified
                    ARS User, never silently replace another ORCID that
                    may already belong to that same account.
                */
                if (existingUser != null &&
                    !string.IsNullOrWhiteSpace(existingUser.OrcidId) &&
                    !string.Equals(
                        existingUser.OrcidId,
                        verifiedOrcidId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        "This ARS account already has another ORCID iD connected.");
                }
            }

            var otp = GenerateOtp();

            User user;

            if (existingUser != null)
            {
                user = existingUser;

                user.FullName = request.FullName;
                user.PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        request.Password);

                user.IsActive = false;
                user.IsEmailVerified = false;
                user.VerificationStatus = "Pending";
                user.ProofDocumentUrl =
                    request.PdfUrl.Trim();

                user.UpdatedAt = now;
                user.IsOtpUsed = false;
                user.OtpCode = otp;
                user.ExpiresOtpAt =
                    now.AddMinutes(5);

                user.ExpiresAt =
                    now.AddDays(7);

                /*
                    Only a verified OAuth ticket is allowed
                    to write these fields.
                */
                if (!string.IsNullOrWhiteSpace(
                        verifiedOrcidId))
                {
                    user.OrcidId =
                        verifiedOrcidId;

                    user.OrcidDisplayName =
                        verifiedOrcidDisplayName;

                    user.IsOrcidVerified =
                        true;

                    user.OrcidVerifiedAt =
                        now;
                }

                _userRepository.Update(user);
            }
            else
            {
                user =
                    _mapper.Map<User>(request);

                user.PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        request.Password);

                user.IsActive = false;
                user.IsEmailVerified = false;
                user.VerificationStatus = "Pending";
                user.ProofDocumentUrl =
                    request.PdfUrl.Trim();

                user.CreatedAt = now;
                user.UpdatedAt = now;

                user.IsOtpUsed = false;
                user.OtpCode = otp;

                user.ExpiresOtpAt =
                    now.AddMinutes(5);

                user.ExpiresAt =
                    now.AddDays(7);

                user.UserRoles =
                    new List<UserRole>();

                /*
                    New account without ORCID:
                        OrcidId remains NULL
                        IsOrcidVerified remains false

                    New account with valid OAuth ticket:
                        persist the authenticated ORCID.
                */
                if (!string.IsNullOrWhiteSpace(
                        verifiedOrcidId))
                {
                    user.OrcidId =
                        verifiedOrcidId;

                    user.OrcidDisplayName =
                        verifiedOrcidDisplayName;

                    user.IsOrcidVerified =
                        true;

                    user.OrcidVerifiedAt =
                        now;
                }

                await _userRepository.AddAsync(user);

                var wallet = new Wallet
                {
                    User = user,
                    Balance = 0,
                    UpdatedAt = now
                };

                await _walletRepository.AddAsync(wallet);
            }

            // Create pending role request for Admin review.
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

            await _roleRequestRepository
                .AddAsync(roleRequest);

            /*
                Consume the ORCID one-time registration ticket.

                This is done before SaveChanges so User + RoleRequest +
                OrcidLinkSession are committed by the same scoped
                AppDbContext SaveChanges operation.
            */
            if (orcidRegistrationSession != null)
            {
                orcidRegistrationSession.Status =
                    "COMPLETED";

                orcidRegistrationSession.CompletedAt =
                    now;

                orcidRegistrationSession.FailureCode =
                    null;

                _orcidLinkSessionRepository
                    .Update(orcidRegistrationSession);
            }

            // Persist registration atomically.
            await _userRepository.SaveChangesAsync();

            // Send OTP email
            try
            {
                var emailBody =
                    BuildOtpEmailBody(
                        user.FullName,
                        otp);

                await _emailService.SendEmailAsync(
                    user.Email,
                    "[ARS] Your OTP Code for Account Registration",
                    emailBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[EMAIL_ERROR] Failed to send OTP email to {user.Email}: {ex.Message}");

                throw new Exception(
                    $"Account created/updated, but failed to send OTP email: {ex.Message}");
            }

            var createdUser =
                await _userRepository
                    .GetWithRoleByIdAsync(
                        user.UserId);

            if (createdUser == null)
                return null;

            var token =
                GenerateJwtToken(
                    createdUser,
                    "Guest");

            return new AuthResponse
            {
                UserId = createdUser.UserId,
                Token = token,
                Username = createdUser.FullName,
                Email = createdUser.Email,
                Role = "Guest",
                IsEmailVerified = createdUser.IsEmailVerified,
                IsActive = createdUser.IsActive,
                VerificationStatus =
                    createdUser.VerificationStatus,
                ExpiresAt = createdUser.ExpiresAt
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

            // Chuẩn hoá role từ FE (coi "null", "" hoặc whitespace là không truyền)
            var incomingRole = string.IsNullOrWhiteSpace(request.Role) || request.Role.Trim().Equals("null", StringComparison.OrdinalIgnoreCase)
                ? null
                : request.Role.Trim();

            // Pending → trả Guest token
            if (string.Equals(user.VerificationStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                var guestToken = GenerateJwtToken(user, "Guest");

                return new AuthResponse
                {
                    UserId = user.UserId,
                    Token = guestToken,
                    Username = user.FullName,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = "Guest",
                    IsEmailVerified = user.IsEmailVerified,
                    IsActive = user.IsActive,
                    VerificationStatus = user.VerificationStatus,
                    IsNewUser = false,
                    RequiresOnboarding = false,
                    EffectiveRole = "Guest",
                    Roles = new List<string> { "Guest" },
                    ExpiresAt = user.ExpiresAt
                };
            }

            if (user.IsActive == false)
                return null;

            string? effectiveRole;

            if (string.Equals(incomingRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // FE yêu cầu role Admin → vào thẳng, không cần check UserRole
                effectiveRole = "Admin";
            }
            else if (!string.IsNullOrEmpty(incomingRole))
            {
                // FE yêu cầu 1 role cụ thể (Reviewer, Researcher...) → check trong UserRole
                var hasRole = await _userRoleRepository.UserHasRoleAsync(user.UserId, incomingRole);
                if (!hasRole)
                    return null; // User không có role này

                effectiveRole = incomingRole;
            }
            else
            {
                // FE không truyền role (hoặc truyền "null") → tự check Admin trong UserRole
                var isAdmin = await _userRoleRepository.UserHasRoleAsync(user.UserId, "Admin");
                if (isAdmin)
                {
                    effectiveRole = "Admin";
                }
                else
                {
                    // Không có Admin → lấy role đầu tiên; nếu rỗng thì trả về Guest
                    var firstRole = user.UserRoles.FirstOrDefault()?.Role?.Name;
                    effectiveRole = string.IsNullOrEmpty(firstRole) ? "Guest" : firstRole;
                }
            }

            var finalToken = GenerateJwtToken(user, effectiveRole);

            var rolesList = user.UserRoles != null && user.UserRoles.Any()
                ? user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).ToList()
                : (!string.IsNullOrEmpty(effectiveRole) ? new List<string> { effectiveRole } : new List<string>());

            if (!string.IsNullOrEmpty(effectiveRole) && !rolesList.Contains(effectiveRole))
            {
                rolesList.Add(effectiveRole);
            }

            return new AuthResponse
            {
                UserId = user.UserId,
                Token = finalToken,
                Username = user.FullName,
                FullName = user.FullName,
                Email = user.Email,
                Role = effectiveRole,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                VerificationStatus = string.Equals(user.VerificationStatus, "Approved", StringComparison.OrdinalIgnoreCase) ? "Accepted" : user.VerificationStatus,
                IsNewUser = false,
                RequiresOnboarding = false,
                EffectiveRole = effectiveRole,
                Roles = rolesList,
                ExpiresAt = user.ExpiresAt
            };
        }

        public async Task<AuthResponse?> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var googleSettings = _configuration.GetSection("GoogleAuth");
            var clientId = googleSettings["ClientId"];

            if (string.IsNullOrEmpty(clientId) || clientId.Contains("REPLACE_WITH"))
            {
                clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            }

            if (string.IsNullOrEmpty(clientId))
                throw new Exception("Google ClientId is not configured.");

            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                if (handler.CanReadToken(request.Credential))
                {
                    var jwtToken = handler.ReadJwtToken(request.Credential);
                    Console.WriteLine($"[GoogleLoginAsync] Received token with Audiences: '{string.Join(",", jwtToken.Audiences)}'");
                    foreach (var aud in jwtToken.Audiences)
                    {
                        Console.WriteLine($"[GoogleLoginAsync] Received token with Audiences list: '{aud}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleLoginAsync] Failed to parse JWT token manually: {ex.Message}");
            }

            GoogleJsonWebSignature.Payload? payload;
            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = GetAcceptedGoogleAudiences(request.Credential)
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, validationSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleLoginAsync] Google token validation failed using Client ID '{clientId}': {ex}");
                return null;
            }

            var user = await _userRepository.GetByUsernameAsync(payload.Email);
            string effectiveRole;

            if (user == null)
            {
                var now = DateTime.UtcNow;
                user = new User
                {
                    Email = payload.Email,
                    FullName = payload.Name ?? payload.Email.Split('@')[0],
                    PasswordHash = string.Empty,
                    IsActive = false,
                    IsEmailVerified = true,
                    VerificationStatus = null,
                    GoogleId = payload.Subject,
                    CreatedAt = now,
                    UpdatedAt = now,
                    UserRoles = new List<UserRole>()
                };

                await _userRepository.AddAsync(user);

                var professionalProfile = new ProfessionalProfile
                {
                    User = user,
                    SyncStatus = "pending",
                    UpdatedAt = now
                };
                await _professionalProfileRepository.AddAsync(professionalProfile);

                var wallet = new Wallet
                {
                    User = user,
                    Balance = 0,
                    UpdatedAt = now
                };
                await _walletRepository.AddAsync(wallet);

                await _userRepository.SaveChangesAsync();

                var onboardingToken = GenerateJwtToken(user, null);

                return new AuthResponse
                {
                    UserId = user.UserId,
                    Token = onboardingToken,
                    Username = user.FullName,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = null,
                    IsEmailVerified = user.IsEmailVerified,
                    IsActive = user.IsActive,
                    VerificationStatus = null,
                    IsNewUser = true,
                    RequiresOnboarding = true,
                    EffectiveRole = null,
                    Roles = new List<string>(),
                    ExpiresAt = user.ExpiresAt
                };
            }
            else
            {
                if (user.IsActive == false && !string.Equals(user.VerificationStatus, "Pending", StringComparison.OrdinalIgnoreCase) && user.VerificationStatus != null)
                    return null;

                if (string.Equals(user.VerificationStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                    return null;

                var hasOnboarded = await _roleRequestRepository.ExistsAsync(rr =>
                    rr.User.UserId == user.UserId &&
                    (rr.Status == "PENDING" || rr.Status == "APPROVED"));
                var hasRoles = user.UserRoles != null && user.UserRoles.Any();

                // If user has not submitted onboarding yet, force them to the onboarding page
                if (!hasRoles && !hasOnboarded)
                {
                    var onboardingToken = GenerateJwtToken(user, null);
                    return new AuthResponse
                    {
                        UserId = user.UserId,
                        Token = onboardingToken,
                        Username = user.FullName,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = null,
                        IsEmailVerified = user.IsEmailVerified,
                        IsActive = user.IsActive,
                        VerificationStatus = null,
                        IsNewUser = true,
                        RequiresOnboarding = true,
                        EffectiveRole = null,
                        Roles = new List<string>(),
                        ExpiresAt = user.ExpiresAt
                    };
                }

                // If user has submitted onboarding and is awaiting Admin approval, let them view Forum as Guest
                if (string.Equals(user.VerificationStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    var guestToken = GenerateJwtToken(user, "Guest");
                    return new AuthResponse
                    {
                        UserId = user.UserId,
                        Token = guestToken,
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
                        Roles = new List<string>(),
                        ExpiresAt = user.ExpiresAt
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
                FullName = user.FullName,
                Email = user.Email,
                Role = effectiveRole,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                VerificationStatus = string.Equals(user.VerificationStatus, "Approved", StringComparison.OrdinalIgnoreCase) ? "Accepted" : user.VerificationStatus,
                IsNewUser = false,
                RequiresOnboarding = false,
                EffectiveRole = effectiveRole,
                Roles = rolesList,
                ExpiresAt = user.ExpiresAt
            };
        }

        private string GenerateJwtToken(User user, string? effectiveRole = null)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var keyString = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings["Key"] ?? "ARSPlatformSuperSecretKeyThatIsAtLeast32BytesLong!";
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
            var keyString = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings["Key"] ?? "ARSPlatformSuperSecretKeyThatIsAtLeast32BytesLong!";
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

        private static string ComputeSha256(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Value cannot be empty.",
                    nameof(value));
            }

            var bytes =
                Encoding.UTF8.GetBytes(value);

            var hash =
                SHA256.HashData(bytes);

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<bool> VerifyOtpAsync(string email, string otpCode)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return false;

            if (user.ExpiresOtpAt == null || user.ExpiresOtpAt < DateTime.UtcNow)
                throw new Exception("This OTP has expired.");

            if (string.IsNullOrEmpty(user.OtpCode) || user.OtpCode != otpCode.Trim())
                return false;

            user.IsOtpUsed = true;
            user.IsEmailVerified = true;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        public async Task<string?> ResendOtpAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return null;

            var now = DateTime.UtcNow;
            var newOtp = GenerateOtp();

            user.OtpCode = newOtp;
            user.ExpiresOtpAt = now.AddMinutes(5);
            user.IsOtpUsed = false;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            var emailBody = BuildOtpEmailBody(user.FullName, newOtp);
            await _emailService.SendEmailAsync(
                email,
                "[ARS] Your New OTP Code",
                emailBody);
            return newOtp;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim());
            if (user == null)
                throw new Exception("User with this email does not exist.");

            var now = DateTime.UtcNow;
            var otp = GenerateOtp();

            user.OtpCode = otp;
            user.ExpiresOtpAt = now.AddMinutes(5);
            user.IsOtpUsed = false;
            user.UpdatedAt = now;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            var emailBody = BuildForgotPasswordEmailBody(user.FullName, otp);
            await _emailService.SendEmailAsync(
                user.Email,
                "[ARS] Password Reset OTP Code",
                emailBody);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (!string.IsNullOrEmpty(request.ConfirmPassword) && request.NewPassword != request.ConfirmPassword)
                throw new Exception("New password and confirm password do not match.");

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                throw new Exception("New password must be at least 6 characters.");

            var user = await _userRepository.GetByEmailAsync(request.Email.Trim());
            if (user == null)
                throw new Exception("User with this email does not exist.");

            if (user.ExpiresOtpAt == null || user.ExpiresOtpAt < DateTime.UtcNow)
                throw new Exception("This OTP has expired.");

            if (string.IsNullOrEmpty(user.OtpCode) || user.OtpCode != request.OtpCode.Trim())
                throw new Exception("Invalid OTP code.");

            // Update password hash
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Delete / Clear OTP code from database after successful password reset
            user.OtpCode = null;
            user.ExpiresOtpAt = null;
            user.IsOtpUsed = true;
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        private string BuildForgotPasswordEmailBody(string fullName, string otp)
        {
            var safeFullName = HtmlEncoder.Default.Encode(fullName);
            var safeOtp = HtmlEncoder.Default.Encode(otp);

            return $@"
<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><!--[if mso]>
  <style type=""text/css"">
    body, table, td, p, a, h1, h2, h3 {{ font-family: Arial, Helvetica, sans-serif !important; }}
  </style>
  <![endif]-->
  <style type=""text/css"">
    @media screen and (max-width: 620px) {{
      .email-shell {{ width: 100% !important; }}
      .email-gutter {{ padding-left: 24px !important; padding-right: 24px !important; }}
      .otp-box {{ padding-left: 20px !important; padding-right: 20px !important; }}
      .otp-code {{ font-size: 28px !important; letter-spacing: 6px !important; }}
    }}
  </style>
</head>
<body style=""margin:0; padding:0; background-color:#f5f1e8; color:#1d1c19; font-family:Arial, Helvetica, sans-serif;""><span style=""display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;"">Your ARS password reset code is ready. It expires in 5 minutes.</span>
  <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#f5f1e8;""><tr><td align=""center"" style=""padding:36px 16px;"">
    <table role=""presentation"" class=""email-shell"" width=""600"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""width:100%; max-width:600px; background-color:#ffffff; border:1px solid #ded9cf;""><tr><td>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#1d1c19;""><tr><td class=""email-gutter"" style=""padding:26px 40px 24px;"">
        <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:25px; line-height:30px; font-weight:bold; color:#fffdf8;"">ARS<span style=""color:#e2ad2f;"">.</span></div>
        <div style=""padding-top:7px; font-size:10px; line-height:14px; letter-spacing:2px; color:#d7d2c8;"">ACADEMIC RESEARCH SHARING</div>
      </td></tr></table>
      <div style=""height:4px; line-height:4px; font-size:4px; background-color:#e2ad2f;"">&nbsp;</div>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0""><tr><td class=""email-gutter"" style=""padding:40px;"">
        <div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Password reset</div>
        <h1 style=""margin:9px 0 18px; font-family:Georgia, 'Times New Roman', serif; font-size:30px; line-height:36px; font-weight:bold; color:#1d1c19;"">Hello {safeFullName},</h1>
        <p style=""margin:0; font-size:16px; line-height:26px; color:#4f4b42;"">You requested a password reset for your Academic Research Sharing account. Enter the verification code below to continue.</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin:30px 0 18px;""><tr><td align=""center"" class=""otp-box"" style=""padding:22px 28px; background-color:#fff9e8; border:1px solid #e2ad2f;""><div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Your one-time code</div><div class=""otp-code"" style=""padding-top:8px; font-family:'Courier New', Courier, monospace; font-size:32px; line-height:40px; font-weight:bold; letter-spacing:8px; color:#1d1c19;"">{safeOtp}</div></td></tr></table>
        <p style=""margin:0; text-align:center; font-size:13px; line-height:20px; color:#6f695d;"">This code expires in <strong style=""color:#4f4b42;"">5 minutes</strong>.</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin-top:30px;""><tr><td style=""padding:16px; background-color:#f6f8f5; border:1px solid #d7ded7;""><p style=""margin:0 0 6px; font-size:13px; line-height:19px; font-weight:bold; color:#4f765d;"">Keep your account secure</p><p style=""margin:0; font-size:13px; line-height:20px; color:#4f4b42;"">If you did not request a password reset, you can ignore this email. Never share this code with anyone.</p></td></tr></table>
      </td></tr></table>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""border-top:1px solid #ded9cf; background-color:#fffdf8;""><tr><td class=""email-gutter"" align=""center"" style=""padding:20px 40px;""><p style=""margin:0; font-size:12px; line-height:19px; color:#6f695d;"">Academic Research Sharing Platform</p><p style=""margin:5px 0 0; font-size:12px; line-height:19px; color:#6f695d;"">This message was sent automatically. Please do not reply.</p></td></tr></table>
    </td></tr></table>
  </td></tr></table>
</body>
</html>";
        }

        private string BuildOtpEmailBody(string fullName, string otp)
        {
            var safeFullName = HtmlEncoder.Default.Encode(fullName);
            var safeOtp = HtmlEncoder.Default.Encode(otp);

            return $@"
<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><!--[if mso]>
  <style type=""text/css"">
    body, table, td, p, a, h1, h2, h3 {{ font-family: Arial, Helvetica, sans-serif !important; }}
  </style>
  <![endif]-->
  <style type=""text/css"">
    @media screen and (max-width: 620px) {{
      .email-shell {{ width: 100% !important; }}
      .email-gutter {{ padding-left: 24px !important; padding-right: 24px !important; }}
      .otp-box {{ padding-left: 20px !important; padding-right: 20px !important; }}
      .otp-code {{ font-size: 28px !important; letter-spacing: 6px !important; }}
    }}
  </style>
</head>
<body style=""margin:0; padding:0; background-color:#f5f1e8; color:#1d1c19; font-family:Arial, Helvetica, sans-serif;""><span style=""display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;"">Your ARS account verification code is ready. It expires in 5 minutes.</span>
  <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#f5f1e8;""><tr><td align=""center"" style=""padding:36px 16px;"">
    <table role=""presentation"" class=""email-shell"" width=""600"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""width:100%; max-width:600px; background-color:#ffffff; border:1px solid #ded9cf;""><tr><td>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#1d1c19;""><tr><td class=""email-gutter"" style=""padding:26px 40px 24px;"">
        <div style=""font-family:Georgia, 'Times New Roman', serif; font-size:25px; line-height:30px; font-weight:bold; color:#fffdf8;"">ARS<span style=""color:#e2ad2f;"">.</span></div>
        <div style=""padding-top:7px; font-size:10px; line-height:14px; letter-spacing:2px; color:#d7d2c8;"">ACADEMIC RESEARCH SHARING</div>
      </td></tr></table>
      <div style=""height:4px; line-height:4px; font-size:4px; background-color:#e2ad2f;"">&nbsp;</div>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0""><tr><td class=""email-gutter"" style=""padding:40px;"">
        <div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Account verification</div>
        <h1 style=""margin:9px 0 18px; font-family:Georgia, 'Times New Roman', serif; font-size:30px; line-height:36px; font-weight:bold; color:#1d1c19;"">Welcome to ARS, {safeFullName}</h1>
        <p style=""margin:0; font-size:16px; line-height:26px; color:#4f4b42;"">Thank you for creating an Academic Research Sharing account. Enter the verification code below to confirm your email address.</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin:30px 0 18px;""><tr><td align=""center"" class=""otp-box"" style=""padding:22px 28px; background-color:#fff9e8; border:1px solid #e2ad2f;""><div style=""font-size:11px; line-height:16px; letter-spacing:1.5px; text-transform:uppercase; color:#6f695d;"">Your one-time code</div><div class=""otp-code"" style=""padding-top:8px; font-family:'Courier New', Courier, monospace; font-size:32px; line-height:40px; font-weight:bold; letter-spacing:8px; color:#1d1c19;"">{safeOtp}</div></td></tr></table>
        <p style=""margin:0; text-align:center; font-size:13px; line-height:20px; color:#6f695d;"">This code expires in <strong style=""color:#4f4b42;"">5 minutes</strong>.</p>
        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin-top:30px;""><tr><td style=""padding:16px; background-color:#f6f8f5; border:1px solid #d7ded7;""><p style=""margin:0 0 6px; font-size:13px; line-height:19px; font-weight:bold; color:#4f765d;"">Keep your account secure</p><p style=""margin:0; font-size:13px; line-height:20px; color:#4f4b42;"">If you did not create an ARS account, you can ignore this email. Never share this code with anyone.</p></td></tr></table>
      </td></tr></table>
      <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""border-top:1px solid #ded9cf; background-color:#fffdf8;""><tr><td class=""email-gutter"" align=""center"" style=""padding:20px 40px;""><p style=""margin:0; font-size:12px; line-height:19px; color:#6f695d;"">Academic Research Sharing Platform</p><p style=""margin:5px 0 0; font-size:12px; line-height:19px; color:#6f695d;"">This message was sent automatically. Please do not reply.</p></td></tr></table>
    </td></tr></table>
  </td></tr></table>
</body>
</html>";
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var keyString = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings["Key"] ?? "ARSPlatformSuperSecretKeyThatIsAtLeast32BytesLong!";
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
                Audience = GetAcceptedGoogleAudiences(idToken)
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            var requestDto = new GoogleLoginRequest { Credential = idToken };
            return await GoogleLoginAsync(requestDto);
        }

        public async Task<AuthResponse?> CompleteGoogleRegistrationAsync(CompleteGoogleRegistrationRequest request)
        {
            var googleSettings = _configuration.GetSection("GoogleAuth");
            var clientId = googleSettings["ClientId"];

            if (string.IsNullOrEmpty(clientId) || clientId.Contains("REPLACE_WITH"))
            {
                clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            }

            if (string.IsNullOrEmpty(clientId))
                throw new Exception("Google ClientId is not configured.");

            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                if (handler.CanReadToken(request.Credential))
                {
                    var jwtToken = handler.ReadJwtToken(request.Credential);
                    Console.WriteLine($"[CompleteGoogleRegistrationAsync] Received token with Audiences: '{string.Join(",", jwtToken.Audiences)}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CompleteGoogleRegistrationAsync] Failed to parse JWT token manually: {ex.Message}");
            }

            GoogleJsonWebSignature.Payload? payload;
            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = GetAcceptedGoogleAudiences(request.Credential)
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, validationSettings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid Google credential: {ex.Message}");
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

            var user = await _userRepository.GetByUsernameAsync(payload.Email);

            if (user == null)
            {
                user = new User
                {
                    Email = payload.Email,
                    FullName = payload.Name ?? payload.Email.Split('@')[0],
                    PasswordHash = string.Empty,
                    IsActive = false,
                    IsEmailVerified = true,
                    VerificationStatus = "Pending",
                    ProofDocumentUrl = request.PdfUrl.Trim(),
                    GoogleId = payload.Subject,
                    CreatedAt = now,
                    UpdatedAt = now,
                    UserRoles = new List<UserRole>()
                };

                await _userRepository.AddAsync(user);

                var professionalProfile = new ProfessionalProfile
                {
                    User = user,
                    SyncStatus = "pending",
                    UpdatedAt = now
                };
                await _professionalProfileRepository.AddAsync(professionalProfile);

                var wallet = new Wallet
                {
                    User = user,
                    Balance = 0,
                    UpdatedAt = now
                };
                await _walletRepository.AddAsync(wallet);
            }
            else
            {
                var existingRequests = await _roleRequestRepository.ExistsAsync(rr =>
                    rr.User.UserId == user.UserId &&
                    (rr.Status == "PENDING" || rr.Status == "APPROVED"));

                if (existingRequests)
                {
                    throw new Exception("A role request already exists or has already been approved.");
                }

                user.ProofDocumentUrl = request.PdfUrl.Trim();
                user.VerificationStatus = "Pending";
                user.IsActive = false;
                user.UpdatedAt = now;
            }

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
                Roles = new List<string>(),
                ExpiresAt = user.ExpiresAt
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
                VerificationStatus = string.Equals(user.VerificationStatus, "Approved", StringComparison.OrdinalIgnoreCase) ? "Accepted" : user.VerificationStatus,
                IsNewUser = false,
                RequiresOnboarding = false,
                EffectiveRole = chosenRole,
                Roles = rolesList,
                ExpiresAt = user.ExpiresAt
            };
        }

        private IEnumerable<string> GetAcceptedGoogleAudiences(string? token = null)
        {
            var audiences = new List<string>
            {
                "900095631091-0u6kiosgvmgf9j7ujrpodkms46k8sfbb.apps.googleusercontent.com",
                "782594816534-gi0o9s6qdbdbmvg7hv52uafe6m7svlol.apps.googleusercontent.com",
                "782594816534-i1sfjl4ostsab3cnkegivtun896fb8b3.apps.googleusercontent.com"
            };

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwtToken = handler.ReadJwtToken(token);
                        foreach (var aud in jwtToken.Audiences)
                        {
                            if (aud.StartsWith("782594816534-") || aud.StartsWith("900095631091-"))
                            {
                                audiences.Add(aud);
                            }
                        }
                    }
                }
                catch { }
            }

            var googleSettings = _configuration.GetSection("GoogleAuth");
            var clientId = googleSettings["ClientId"];
            if (!string.IsNullOrEmpty(clientId) && !clientId.Contains("REPLACE_WITH"))
            {
                audiences.Add(clientId);
            }

            var envClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            if (!string.IsNullOrEmpty(envClientId))
            {
                audiences.Add(envClientId);
            }

            var meetClientId = Environment.GetEnvironmentVariable("GoogleMeetSettings__ClientId")
                ?? _configuration["GoogleMeetSettings:ClientId"];
            if (!string.IsNullOrEmpty(meetClientId))
            {
                audiences.Add(meetClientId);
            }

            return audiences.Distinct();
        }

        public async Task<bool> UpdateExpiresAtAsync(int userId, DateTime expiresAt)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            user.ExpiresAt = expiresAt;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }
    }
}