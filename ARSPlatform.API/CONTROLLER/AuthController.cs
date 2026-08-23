using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
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

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

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

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized(new { Message = "Invalid username or password." });

            return Ok(result);
        }

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

        [HttpPost("complete-google-registration")]
        [Authorize]
        public async Task<IActionResult> CompleteGoogleRegistration([FromBody] CompleteGoogleRegistrationRequest request)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { Message = "User is not authenticated." });
            }

            try
            {
                var result = await _authService.CompleteGoogleRegistrationAsync(userId, request);
                if (result == null)
                    return BadRequest(new { Message = "Failed to complete Google registration." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

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

        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _authService.VerifyEmailAsync(token);
            if (!result)
                return BadRequest(new { Message = "Email verification failed. Invalid or expired token." });

            return Ok(new { Message = "Email verified successfully!" });
        }

        [HttpPost("send-approval-email")]
        public async Task<IActionResult> SendApprovalEmail([FromQuery] string email)
        {
            var result = await _authService.SendApprovalEmailAsync(email);
            if (!result)
                return BadRequest(new { Message = "Failed to send approval email. User not found." });

            return Ok(new { Message = "Approval email sent successfully!" });
        }

        [HttpGet("google-oauth-login")]
        [AllowAnonymous]
        public IActionResult GoogleOAuthLogin()
        {
            var scheme = Request.Host.Host.Contains("localhost") ? Request.Scheme : "https";
            var redirectUri = $"{scheme}://{Request.Host}/api/Auth/google-callback";
            var url = _authService.GetGoogleAuthorizationUrl(redirectUri, "openid profile email");
            return Redirect(url);
        }

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
    }
}