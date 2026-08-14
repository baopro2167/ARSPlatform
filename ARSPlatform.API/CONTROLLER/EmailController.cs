using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send-test")]
        public async Task<IActionResult> SendTestEmail([FromQuery] string toEmail, [FromQuery] string subject, [FromQuery] string body)
        {
            try
            {
                await _emailService.SendEmailAsync(toEmail, subject, body);
                return Ok(new { Message = $"Test email sent successfully to {toEmail}" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = $"Failed to send email: {ex.Message}", Details = ex.InnerException?.Message });
            }
        }
    }
}
