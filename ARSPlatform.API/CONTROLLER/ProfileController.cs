using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using AutoMapper;
using ProfileEntity = ARSPlatform.MODEL.Entities.Profile;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileRepository _repository;
        private readonly IMapper _mapper;

        public ProfileController(IProfileRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllWithUserAsync();
            var response = _mapper.Map<IEnumerable<ProfileResponse>>(items);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProfileCreateRequest request)
        {
            var existing = await _repository.GetByIdAsync(request.UserId);

            if (existing != null)
            {
                return Conflict(new
                {
                    Message = "A profile already exists for this user."
                });
            }

            var item = _mapper.Map<ProfileEntity>(request);

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithUserAsync(item.UserId);
            var response = _mapper.Map<ProfileResponse>(created);

            return CreatedAtAction(
                nameof(GetById),
                new { id = item.UserId },
                response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository.GetByIdWithUserAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var response = _mapper.Map<ProfileResponse>(item);
            return Ok(response);
        }

        [HttpPut("{id:int}")]
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] ProfileUpdateRequest request)
        {
            if (request.UserId != id)
            {
                return BadRequest(new
                {
                    Message = "The request UserId must match the route id."
                });
            }

            var item = await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            _mapper.Map(request, item);

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithUserAsync(id);
            var response = _mapper.Map<ProfileResponse>(updated);

            return Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();

            return NoContent();
        }
    }
}