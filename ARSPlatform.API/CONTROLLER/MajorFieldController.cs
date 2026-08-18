using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
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
    public class MajorFieldController : ControllerBase
    {
        private readonly IMajorFieldRepository _repository;
        private readonly IMapper _mapper;

        public MajorFieldController(IMajorFieldRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllWithSubFieldsAsync();
            var response = _mapper.Map<IEnumerable<MajorFieldResponse>>(items);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MajorFieldCreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    Message = "Major field name is required."
                });
            }

            var normalizedName = request.Name.Trim();

            var exists = await _repository.ExistsAsync(
                x => x.Name == normalizedName);

            if (exists)
            {
                return Conflict(new
                {
                    Message =
                        "A major field with the same name already exists."
                });
            }

            var item = _mapper.Map<MajorField>(request);
            item.Name = normalizedName;
            item.CreatedAt ??= DateTime.UtcNow;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created =
                await _repository.GetByIdWithSubFieldsAsync(
                    item.MajorFieldId);

            var response =
                _mapper.Map<MajorFieldResponse>(created);

            return CreatedAtAction(
                nameof(GetById),
                new { id = item.MajorFieldId },
                response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item =
                await _repository.GetByIdWithSubFieldsAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var response =
                _mapper.Map<MajorFieldResponse>(item);

            return Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] MajorFieldUpdateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    Message = "Major field name is required."
                });
            }

            var item =
                await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var normalizedName = request.Name.Trim();

            var duplicate =
                await _repository.ExistsAsync(
                    x =>
                        x.MajorFieldId != id &&
                        x.Name == normalizedName);

            if (duplicate)
            {
                return Conflict(new
                {
                    Message =
                        "A major field with the same name already exists."
                });
            }

            _mapper.Map(request, item);
            item.Name = normalizedName;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated =
                await _repository.GetByIdWithSubFieldsAsync(id);

            var response =
                _mapper.Map<MajorFieldResponse>(updated);

            return Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item =
                await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            if (await _repository.HasSubFieldsAsync(id))
            {
                return Conflict(new
                {
                    Message =
                        "The major field cannot be deleted while it still contains sub-fields."
                });
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();

            return NoContent();
        }
    }
}