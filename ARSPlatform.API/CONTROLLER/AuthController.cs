using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Claims;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Configuration;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IOrcidLinkService _orcidLinkService;

        public AuthController(
            IAuthService authService,
            IConfiguration configuration,
            IOrcidLinkService orcidLinkService)
        {
            _authService = authService;
            _configuration = configuration;
            _orcidLinkService = orcidLinkService;
        }

        /// <summary>
        /// Đăng ký tài khoản người dùng mới (Local account)
        /// </summary>
        /// <param name="request">Thông tin đăng ký</param>
        /// <returns>Kết quả đăng ký</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                if (result == null)
                    return BadRequest(new { Message = "Registration failed." });

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { Message = message });
            }
        }

        /// <summary>
        /// Đăng nhập hệ thống bằng tài khoản email &amp; mật khẩu
        /// </summary>
        /// <param name="request">Thông tin đăng nhập</param>
        /// <returns>Token JWT và thông tin tài khoản</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized(new { Message = "Invalid username or password." });

            return Ok(result);
        }

        /// <summary>
        /// Đăng nhập hoặc đăng ký nhanh bằng Google ID Token (Google One-Tap / Popup)
        /// </summary>
        /// <param name="request">Google Credential Token</param>
        /// <returns>Token xác thực và thông tin Onboarding</returns>
        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var result = await _authService.GoogleLoginAsync(request);
                if (result == null)
                    return Unauthorized(new { Message = "Invalid Google token or user is not allowed to login." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Hoàn tất Onboarding cho tài khoản Google lần đầu (chọn Role và tải file PDF xác minh)
        /// </summary>
        /// <param name="request">Thông tin Onboarding</param>
        /// <returns>Token Guest và trạng thái chờ duyệt</returns>
        [HttpPost("complete-google-registration")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteGoogleRegistration([FromBody] CompleteGoogleRegistrationRequest request)
        {
            try
            {
                var result = await _authService.CompleteGoogleRegistrationAsync(request);
                if (result == null)
                    return BadRequest(new { Message = "Failed to complete Google registration." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lựa chọn vai trò hoạt động (cho tài khoản có nhiều vai trò)
        /// </summary>
        /// <param name="request">Tên vai trò lựa chọn</param>
        /// <returns>Token mới với quyền tương ứng</returns>
        [HttpPost("select-role")]
        [Authorize]
        public async Task<IActionResult> SelectRole([FromBody] SelectRoleRequest request)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { Message = "User is not authenticated." });
            }

            try
            {
                var result = await _authService.SelectRoleAsync(userId, request.Role);
                if (result == null)
                    return BadRequest(new { Message = "Failed to select role." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xác thực địa chỉ email qua Token kích hoạt
        /// </summary>
        /// <param name="token">Token xác thực email</param>
        /// <returns>Thông báo kết quả</returns>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _authService.VerifyEmailAsync(token);
            if (!result)
                return BadRequest(new { Message = "Email verification failed. Invalid or expired token." });

            return Ok(new { Message = "Email verified successfully!" });
        }

        /// <summary>
        /// Gửi email thông báo tài khoản đã được phê duyệt
        /// </summary>
        /// <param name="email">Địa chỉ email người nhận</param>
        /// <returns>Thông báo kết quả</returns>
        [HttpPost("send-approval-email")]
        public async Task<IActionResult> SendApprovalEmail([FromQuery] string email)
        {
            var result = await _authService.SendApprovalEmailAsync(email);
            if (!result)
                return BadRequest(new { Message = "Failed to send approval email. User not found." });

            return Ok(new { Message = "Approval email sent successfully!" });
        }

        /// <summary>
        /// Xác thực OTP để hoàn tất đăng ký tài khoản
        /// </summary>
        /// <param name="request">Email và mã OTP</param>
        /// <returns>Thông báo kết quả</returns>
        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var result = await _authService.VerifyOtpAsync(request.Email, request.OtpCode);
                if (!result)
                    return BadRequest(new { Message = "Invalid OTP code." });

                return Ok(new { Message = "OTP verified successfully! Please proceed to login." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gửi lại mã OTP mới qua email
        /// </summary>
        /// <param name="email">Địa chỉ email người dùng</param>
        /// <returns>Thông báo kết quả</returns>
        [HttpPost("resend-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendOtp([FromQuery] string email)
        {
            try
            {
                var result = await _authService.ResendOtpAsync(email);
                if (result == null)
                    return BadRequest(new { Message = "User not found or email not registered." });

                return Ok(new { Message = "New OTP has been sent to your email." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Yêu cầu gửi mã OTP đặt lại mật khẩu về email (Luồng 1)
        /// </summary>
        /// <param name="request">Email người dùng</param>
        /// <returns>Thông báo kết quả</returns>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var result = await _authService.ForgotPasswordAsync(request);
                return Ok(new { Message = "Password reset OTP has been sent to your email." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xác thực OTP và đặt lại mật khẩu mới, đồng thời xóa OTP khỏi database (Luồng 2)
        /// </summary>
        /// <param name="request">Email, mã OTP và mật khẩu mới</param>
        /// <returns>Thông báo kết quả</returns>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(request);
                return Ok(new { Message = "Password reset successfully! Redirecting to login..." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Bắt đầu liên kết ORCID trong quá trình đăng ký tài khoản mới.
        /// User chưa tồn tại nên OAuth session sẽ có Context = REGISTRATION và UserId = NULL.
        /// </summary>
        /// <returns>ORCID authorization URL để Frontend chuyển hướng người dùng</returns>
        [HttpPost("orcid/registration/start")]
        [AllowAnonymous]
        public async Task<IActionResult> StartOrcidRegistrationLink(
            CancellationToken cancellationToken)
        {
            try
            {
                var result =
                    await _orcidLinkService.StartRegistrationAsync(
                        cancellationToken);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
                when (IsOrcidConfigurationException(ex))
            {
                return StatusCode(
                    503,
                    new
                    {
                        Code = "ORCID_NOT_CONFIGURED",
                        Message = ex.Message
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new
                    {
                        Code = "ORCID_LINK_START_FAILED",
                        Message = ex.Message
                    });
            }
        }

        /// <summary>
        /// Bắt đầu liên kết ORCID cho một ARS User đã đăng nhập.
        /// UserId được lấy từ JWT, không nhận UserId từ Frontend.
        /// </summary>
        /// <returns>ORCID authorization URL để Frontend chuyển hướng người dùng</returns>
        [HttpPost("orcid/account/start")]
        [Authorize(Policy = "AuthenticatedUser")]
        public async Task<IActionResult> StartOrcidAccountLink(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                    userIdValue,
                    out var userId))
            {
                return Unauthorized(
                    new
                    {
                        Code = "UNAUTHENTICATED",
                        Message = "User is not authenticated."
                    });
            }

            try
            {
                var result =
                    await _orcidLinkService
                        .StartAccountLinkAsync(
                            userId,
                            cancellationToken);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new
                    {
                        Code = "USER_NOT_FOUND",
                        Message = ex.Message
                    });
            }
            catch (InvalidOperationException ex)
                when (IsOrcidConfigurationException(ex))
            {
                return StatusCode(
                    503,
                    new
                    {
                        Code = "ORCID_NOT_CONFIGURED",
                        Message = ex.Message
                    });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(
                    new
                    {
                        Code = "ACCOUNT_ALREADY_HAS_ORCID",
                        Message = ex.Message
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new
                    {
                        Code = "ORCID_LINK_START_FAILED",
                        Message = ex.Message
                    });
            }
        }

        /// <summary>
        /// Lấy trạng thái liên kết ORCID của ARS User hiện tại.
        /// UserId được lấy trực tiếp từ JWT.
        /// </summary>
        /// <returns>Trạng thái ORCID của tài khoản đang đăng nhập</returns>
        [HttpGet("orcid/status")]
        [Authorize(Policy = "AuthenticatedUser")]
        public async Task<ActionResult<OrcidStatusResponse>>
            GetOrcidStatus(
                CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                    userIdValue,
                    out var userId))
            {
                return Unauthorized(
                    new
                    {
                        Code = "UNAUTHENTICATED",
                        Message = "User is not authenticated."
                    });
            }

            var result =
                await _orcidLinkService
                    .GetStatusAsync(
                        userId,
                        cancellationToken);

            if (result == null)
            {
                return NotFound(
                    new
                    {
                        Code = "USER_NOT_FOUND",
                        Message = "User was not found."
                    });
            }

            return Ok(result);
        }

        /// <summary>
        /// Callback OAuth từ ORCID.
        /// ORCID redirect authorization code và state về endpoint này.
        /// </summary>
        /// <param name="code">Authorization code từ ORCID</param>
        /// <param name="state">OAuth state</param>
        /// <param name="error">OAuth error nếu user cancel hoặc provider từ chối</param>
        /// <returns>Redirect về Frontend với kết quả ORCID linking</returns>
        [HttpGet("orcid/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> OrcidCallback(
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            CancellationToken cancellationToken)
        {
            OrcidLinkCallbackResponse result;

            try
            {
                result =
                    await _orcidLinkService
                        .HandleCallbackAsync(
                            code,
                            state,
                            error,
                            cancellationToken);
            }
            catch (Exception)
            {
                result =
                    new OrcidLinkCallbackResponse
                    {
                        Success = false,
                        Status = "FAILED",
                        ErrorCode =
                            "ORCID_CALLBACK_FAILED",
                        ErrorMessage =
                            "ORCID callback could not be processed."
                    };
            }

            var frontendCallbackUrl =
                GetOrcidFrontendCallbackUrl();

            var redirectUrl =
                BuildOrcidFrontendRedirectUrl(
                    frontendCallbackUrl,
                    result);

            return Redirect(redirectUrl);
        }

        /// <summary>
        /// Khởi tạo luồng đăng nhập OAuth Google Redirect
        /// </summary>
        /// <returns>Chuyển hướng đến Google OAuth</returns>
        [HttpGet("google-oauth-login")]
        [AllowAnonymous]
        public IActionResult GoogleOAuthLogin()
        {
            var scheme = Request.Host.Host.Contains("localhost") ? Request.Scheme : "https";
            var redirectUri = $"{scheme}://{Request.Host}/api/Auth/google-callback";
            var url = _authService.GetGoogleAuthorizationUrl(redirectUri, "openid profile email");
            return Redirect(url);
        }

        /// <summary>
        /// Callback tiếp nhận mã OAuth từ Google và chuyển hướng về Frontend
        /// </summary>
        /// <param name="code">Mã Authorization code</param>
        /// <param name="error">Lỗi nếu có</param>
        /// <returns>Chuyển hướng về Frontend kèm query parameters</returns>
        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string? error)
        {
            var baseVerifyUrl = _configuration["EmailSettings:VerificationUrl"] ?? "https://fe-ars.vercel.app/verify-email";
            var frontendOrigin = new Uri(baseVerifyUrl).GetLeftPart(UriPartial.Authority);
            var frontendCallbackUrl = $"{frontendOrigin}/auth/google/callback";

            if (!string.IsNullOrEmpty(error))
            {
                return Redirect($"{frontendCallbackUrl}?error={Uri.EscapeDataString(error)}");
            }

            try
            {
                var scheme = Request.Host.Host.Contains("localhost") ? Request.Scheme : "https";
                var redirectUri = $"{scheme}://{Request.Host}/api/Auth/google-callback";
                var result = await _authService.AuthenticateGoogleLoginAsync(code, redirectUri);

                if (result == null)
                {
                    throw new Exception("Google authentication returned no account details.");
                }

                var query = $"?token={Uri.EscapeDataString(result.Token ?? "")}" +
                            $"&userId={result.UserId}" +
                            $"&email={Uri.EscapeDataString(result.Email ?? "")}" +
                            $"&fullName={Uri.EscapeDataString(result.FullName ?? "")}" +
                            $"&isNewUser={(result.IsNewUser ?? false).ToString().ToLower()}" +
                            $"&requiresOnboarding={(result.RequiresOnboarding ?? false).ToString().ToLower()}" +
                            $"&roles={Uri.EscapeDataString(string.Join(",", result.Roles ?? new List<string>()))}" +
                            $"&role={Uri.EscapeDataString(result.Role ?? "")}" +
                            $"&isActive={(result.IsActive ?? false).ToString().ToLower()}" +
                            $"&verificationStatus={Uri.EscapeDataString(result.VerificationStatus ?? "")}" +
                            $"&effectiveRole={Uri.EscapeDataString(result.EffectiveRole ?? "")}";

                return Redirect($"{frontendCallbackUrl}{query}");
            }
            catch (Exception ex)
            {
                return Redirect($"{frontendCallbackUrl}?error={Uri.EscapeDataString(ex.Message)}");
            }
        }

        private string GetOrcidFrontendCallbackUrl()
        {
            var configuredUrl =
                Environment.GetEnvironmentVariable(
                    "ORCID_FRONTEND_CALLBACK_URL")
                ?? _configuration[
                    "OrcidSettings:FrontendCallbackUrl"];

            if (!string.IsNullOrWhiteSpace(
                    configuredUrl))
            {
                return configuredUrl.Trim();
            }

            var baseVerifyUrl =
                _configuration[
                    "EmailSettings:VerificationUrl"]
                ?? "https://fe-ars.vercel.app/verify-email";

            var frontendOrigin =
                new Uri(baseVerifyUrl)
                    .GetLeftPart(
                        UriPartial.Authority);

            return
                $"{frontendOrigin}/auth/orcid/callback";
        }

        private static string
            BuildOrcidFrontendRedirectUrl(
                string frontendCallbackUrl,
                OrcidLinkCallbackResponse result)
        {
            var fragmentParts =
                new List<string>
                {
                    $"success={result.Success.ToString().ToLowerInvariant()}"
                };

            if (!string.IsNullOrWhiteSpace(
                    result.Context))
            {
                fragmentParts.Add(
                    "context="
                    + Uri.EscapeDataString(
                        result.Context));
            }

            if (!string.IsNullOrWhiteSpace(
                    result.Status))
            {
                fragmentParts.Add(
                    "status="
                    + Uri.EscapeDataString(
                        result.Status));
            }

            if (!string.IsNullOrWhiteSpace(
                    result.OrcidId))
            {
                fragmentParts.Add(
                    "orcidId="
                    + Uri.EscapeDataString(
                        result.OrcidId));
            }

            if (!string.IsNullOrWhiteSpace(
                    result.DisplayName))
            {
                fragmentParts.Add(
                    "displayName="
                    + Uri.EscapeDataString(
                        result.DisplayName));
            }

            if (!string.IsNullOrWhiteSpace(
                    result.RegistrationTicket))
            {
                fragmentParts.Add(
                    "registrationTicket="
                    + Uri.EscapeDataString(
                        result.RegistrationTicket));
            }

            if (!string.IsNullOrWhiteSpace(
                    result.ErrorCode))
            {
                fragmentParts.Add(
                    "errorCode="
                    + Uri.EscapeDataString(
                        result.ErrorCode));
            }

            if (!string.IsNullOrWhiteSpace(
                    result.ErrorMessage))
            {
                fragmentParts.Add(
                    "errorMessage="
                    + Uri.EscapeDataString(
                        result.ErrorMessage));
            }

            var baseUrl =
                frontendCallbackUrl
                    .Split('#')[0];

            return
                $"{baseUrl}#{string.Join("&", fragmentParts)}";
        }

        private static bool
            IsOrcidConfigurationException(
                InvalidOperationException exception)
        {
            return
                exception.Message.Contains(
                    "ORCID",
                    StringComparison.OrdinalIgnoreCase)
                &&
                exception.Message.Contains(
                    "not configured",
                    StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Cập nhật thời hạn trải nghiệm (ExpiresAcc) cho tài khoản người dùng
        /// </summary>
        /// <param name="request">UserId và ExpiresAcc mới</param>
        /// <returns>Thông báo kết quả</returns>
        [HttpPut("update-expires-acc")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateExpiresAcc([FromBody] UpdateExpiresAccRequest request)
        {
            try
            {
                var result = await _authService.UpdateExpiresAccAsync(request.UserId, request.ExpiresAcc);
                if (!result)
                    return BadRequest(new { Message = "Failed to update ExpiresAcc." });

                return Ok(new { Message = "ExpiresAcc updated successfully.", ExpiresAcc = request.ExpiresAcc });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}