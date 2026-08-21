using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
            var url = _authService.GetGoogleAuthorizationUrl(redirectUri);
            return Redirect(url);
        }

        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string? error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest(new { Message = $"Google login failed: {error}" });
            }

            try
            {
                var scheme = Request.Host.Host.Contains("localhost") ? Request.Scheme : "https";
                var redirectUri = $"{scheme}://{Request.Host}/api/Auth/google-callback";
                var result = await _authService.ExchangeCodeForRefreshTokenAsync(code, redirectUri);
                
                var htmlContent = $@"
                    <html>
                    <head>
                        <title>Google Authentication Successful</title>
                        <style>
                            body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; background-color: #f4f6f9; }}
                            .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
                            h1 {{ color: #243257; }}
                            p {{ color: #555; }}
                            .token-box {{ background: #eee; padding: 15px; border-radius: 4px; font-family: monospace; word-break: break-all; margin: 20px 0; border: 1px solid #ccc; font-size: 14px; text-align: left; }}
                            .btn {{ background-color: #007aff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h1>Google Auth Successful</h1>
                            <p>Copy the Google Refresh Token below and update it in your Render settings under the key <strong>GoogleMeetSettings__RefreshToken</strong>:</p>
                            <div class='token-box'>{result}</div>
                            <p>After saving on Render, wait for it to deploy and try creating a Seminar again!</p>
                        </div>
                    </body>
                    </html>";
                
                return Content(htmlContent, "text/html");
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}