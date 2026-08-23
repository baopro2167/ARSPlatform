using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoogleMeetController : ControllerBase
    {
        private readonly IAuthService _authService;

        public GoogleMeetController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("authorize")]
        [AllowAnonymous]
        public IActionResult Authorize()
        {
            var scheme = Request.Host.Host.Contains("localhost") ? Request.Scheme : "https";
            var redirectUri = $"{scheme}://{Request.Host}/api/GoogleMeet/callback";
            
            // Require both calendar and meetings space scopes for Meet integration
            var scopes = "https://www.googleapis.com/auth/calendar https://www.googleapis.com/auth/meetings.space.created";
            var url = _authService.GetGoogleAuthorizationUrl(redirectUri, scopes);
            
            return Redirect(url);
        }

        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest(new { Message = $"Google Meet authorization failed: {error}" });
            }

            try
            {
                var scheme = Request.Host.Host.Contains("localhost") ? Request.Scheme : "https";
                var redirectUri = $"{scheme}://{Request.Host}/api/GoogleMeet/callback";
                var result = await _authService.ExchangeCodeForRefreshTokenAsync(code, redirectUri);
                
                var htmlContent = $@"
                    <html>
                    <head>
                        <title>Google Meet Authorization Successful</title>
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
                            <h1>Google Meet Authorization Successful</h1>
                            <p>Copy the Google Refresh Token below and update it in your Render settings under the key <strong>GoogleMeetSettings__RefreshToken</strong>:</p>
                            <div class='token-box'>{result}</div>
                            <p>After saving on Render, wait for it to deploy and try creating a Seminar again!</p>
                        </div>
                    </body>
                    </html>";
                
                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
