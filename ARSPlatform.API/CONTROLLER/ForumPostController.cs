using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForumPostController : ControllerBase
    {
        private readonly IForumPostRepository _repository;
        private readonly IMapper _mapper;

        public ForumPostController(
            IForumPostRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // GET:
        // /api/ForumPost
        // /api/ForumPost?category=AI
        // /api/ForumPost?sort=popular
        // /api/ForumPost?search=RAG
        // /api/ForumPost?category=AI&sort=popular&search=RAG
        [HttpGet]
        [Authorize(Policy = "ForumRead")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? category,
            [FromQuery] string? sort,
            [FromQuery] string? search)
        {
            var items = await _repository.SearchAsync(
                search,
                category,
                sort);

            var response =
                _mapper.Map<IEnumerable<ForumPostResponse>>(items);

            return Ok(response);
        }

        // GET:
        // /api/ForumPost/1
        [HttpGet("{id:int}")]
        [Authorize(Policy = "ForumRead")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.ForumComments)
                .FirstOrDefaultAsync(
                    p => p.ForumPostId == id);

            if (item == null)
                return NotFound();

            var response =
                _mapper.Map<ForumPostResponse>(item);

            return Ok(response);
        }

        // POST:
        // /api/ForumPost
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(
            [FromBody] ForumPostCreateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    Message = "Request body is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new
                {
                    Message = "Content is required."
                });
            }

            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var item =
                _mapper.Map<ForumPost>(request);

            // IMPORTANT:
            // UserId must come from JWT, not FE request body.
            item.UserId = userId;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            // Reload navigation properties so that
            // Author / AuthorAvatar are available.
            var createdItem =
                await _repository
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(p => p.User)
                    .Include(p => p.ForumComments)
                    .FirstOrDefaultAsync(
                        p => p.ForumPostId == item.ForumPostId);

            if (createdItem == null)
            {
                return StatusCode(
                    500,
                    new
                    {
                        Message =
                            "Forum post was created but could not be loaded."
                    });
            }

            var response =
                _mapper.Map<ForumPostResponse>(
                    createdItem);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }
    }
}