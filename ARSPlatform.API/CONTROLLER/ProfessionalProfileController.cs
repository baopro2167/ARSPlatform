using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using AutoMapper;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfessionalProfileController : ControllerBase
    {
        private readonly IProfessionalProfileRepository _repository;
        private readonly IMapper _mapper;

        public ProfessionalProfileController(IProfessionalProfileRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllWithUserAndFieldAsync();
            var response = _mapper.Map<IEnumerable<ProfessionalProfileResponse>>(items);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProfessionalProfileCreateRequest request)
        {
            var item = _mapper.Map<ProfessionalProfile>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithUserAndFieldAsync(item.UserId);
            var response = _mapper.Map<ProfessionalProfileResponse>(created);
            return CreatedAtAction(nameof(GetById), new { id = item.UserId }, response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository.GetByIdWithUserAndFieldAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            var response = _mapper.Map<ProfessionalProfileResponse>(item);
            return Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProfessionalProfileUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithUserAndFieldAsync(id);
            var response = _mapper.Map<ProfessionalProfileResponse>(updated);
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