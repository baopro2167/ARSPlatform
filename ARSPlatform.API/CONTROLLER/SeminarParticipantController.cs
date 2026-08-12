using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SeminarParticipantController : ControllerBase
    {
        private readonly ISeminarParticipantRepository _repository;
        private readonly IMapper _mapper;

        public SeminarParticipantController(
            ISeminarParticipantRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // ==========================================
        // GET: api/SeminarParticipant
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllAsync();

            var response =
                _mapper.Map<IEnumerable<SeminarParticipantResponse>>(items);

            return Ok(response);
        }

        // ==========================================
        // GET: api/SeminarParticipant/{id}
        // ==========================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item =
                await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var response =
                _mapper.Map<SeminarParticipantResponse>(item);

            return Ok(response);
        }

        // ==========================================
        // GET: api/SeminarParticipant/seminar/{seminarId}
        // ==========================================

        [HttpGet("seminar/{seminarId:int}")]
        public async Task<IActionResult> GetBySeminarId(int seminarId)
        {
            var items =
                await _repository.GetBySeminarIdAsync(seminarId);

            var response =
                _mapper.Map<IEnumerable<SeminarParticipantResponse>>(items);

            return Ok(response);
        }

        // ==========================================
        // POST: api/SeminarParticipant
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] SeminarParticipantCreateRequest request)
        {
            var item =
                _mapper.Map<SeminarParticipant>(request);

            var createdItem =
                await _repository.CreateAsync(item);

            var response =
                _mapper.Map<SeminarParticipantResponse>(createdItem);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdItem.SeminarParticipantId },
                response);
        }

        // ==========================================
        // PUT: api/SeminarParticipant/{id}
        // ==========================================

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] SeminarParticipantUpdateRequest request)
        {
            var item =
                await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            _mapper.Map(request, item);

            item.SeminarParticipantId = id;

            var updatedItem =
                await _repository.UpdateAsync(item);

            var response =
                _mapper.Map<SeminarParticipantResponse>(updatedItem);

            return Ok(response);
        }

        // ==========================================
        // DELETE: api/SeminarParticipant/{id}
        // ==========================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted =
                await _repository.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}