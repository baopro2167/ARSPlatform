using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _service;

        public AdminController(IAdminService service)
        {
            _service = service;
        }

        /// <summary>
        /// Tra cứu thông tin tác giả và hồ sơ khoa học thông qua ORCID iD (kết nối OpenAlex)
        /// </summary>
        /// <param name="request">Yêu cầu tra cứu ORCID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Thông tin hồ sơ khoa học từ ORCID / OpenAlex</returns>
        [HttpPost("orcid-lookup")]
        [ProducesResponseType(typeof(OrcidLookupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OrcidLookupResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OrcidLookupResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(OrcidLookupResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(OrcidLookupResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(OrcidLookupResponse), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(OrcidLookupResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> LookupOrcid(
            [FromBody] OrcidLookupRequest? request,
            CancellationToken cancellationToken)
        {
            var adminContext = GetCurrentAdmin();
            if (adminContext == null)
            {
                return Unauthorized();
            }

            if (request == null)
            {
                return BadRequest(new { Message = "Request body is required." });
            }

            var correlationId = HttpContext.TraceIdentifier;
            var (response, statusCode) = await _service.LookupOrcidAsync(
                request,
                adminContext.Value.AdminId,
                adminContext.Value.AdminName,
                correlationId,
                cancellationToken);

            return StatusCode(statusCode, response);
        }

        private (int AdminId, string AdminName)? GetCurrentAdmin()
        {
            var adminIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdValue, out var adminId))
            {
                return null;
            }

            var adminName = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(adminName))
            {
                adminName = $"Admin {adminId}";
            }

            return (adminId, adminName);
        }
    }
}