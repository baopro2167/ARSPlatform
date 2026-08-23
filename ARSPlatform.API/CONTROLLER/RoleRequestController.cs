using System.Security.Claims;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RoleRequestController : ControllerBase
    {
        private readonly IRoleRequestService _roleRequestService;

        public RoleRequestController(
            IRoleRequestService roleRequestService)
        {
            _roleRequestService = roleRequestService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(IEnumerable<RoleRequestResponse>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var items = await _roleRequestService.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _roleRequestService.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound(new
                {
                    Message = $"Role request {id} was not found."
                });
            }

            return Ok(item);
        }

        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(
            int id,
            [FromBody] RoleRequestDecisionRequest? request)
        {
            var adminId = GetCurrentAdminId();

            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            request ??= new RoleRequestDecisionRequest();

            try
            {
                var result = await _roleRequestService.ApproveAsync(
                    id,
                    adminId.Value,
                    request);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpPost("{id:int}/deny")]
        [ProducesResponseType(
            typeof(RoleRequestResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Deny(
            int id,
            [FromBody] RoleRequestDecisionRequest? request)
        {
            var adminId = GetCurrentAdminId();

            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    Message = "Request body is required."
                });
            }

            try
            {
                var result = await _roleRequestService.DenyAsync(
                    id,
                    adminId.Value,
                    request);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        private int? GetCurrentAdminId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(value, out var adminId)
                ? adminId
                : null;
        }
    }
}
