using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaperController : ControllerBase
    {
        private readonly IPaperService _paperService;

        public PaperController(IPaperService paperService)
        {
            _paperService = paperService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPapers([FromQuery] PaginationParams paginationParams)
        {
            var result = await _paperService.GetPapersAsync(paginationParams);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaperById(Guid id)
        {
            var paper = await _paperService.GetPaperByIdAsync(id);
            if (paper == null)
                return NotFound();

            return Ok(paper);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePaper([FromBody] PaperCreateRequest request)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdStr))
                return Unauthorized();

            try
            {
                var authorId = Guid.Parse(currentUserIdStr);
                var createdPaper = await _paperService.CreatePaperAsync(request, authorId);
                return CreatedAtAction(nameof(GetPaperById), new { id = createdPaper.Id }, createdPaper);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePaper(Guid id, [FromBody] PaperUpdateRequest request)
        {
            var paper = await _paperService.GetPaperByIdAsync(id);
            if (paper == null)
                return NotFound();

            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole != "Admin" && paper.AuthorId.ToString() != currentUserIdStr)
            {
                return Forbid();
            }

            try
            {
                var updatedPaper = await _paperService.UpdatePaperAsync(id, request);
                return Ok(updatedPaper);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePaper(Guid id)
        {
            var paper = await _paperService.GetPaperByIdAsync(id);
            if (paper == null)
                return NotFound();

            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (currentUserRole != "Admin" && paper.AuthorId.ToString() != currentUserIdStr)
            {
                return Forbid();
            }

            await _paperService.DeletePaperAsync(id);
            return Ok(new { Message = "Paper deleted successfully." });
        }
    }
}
