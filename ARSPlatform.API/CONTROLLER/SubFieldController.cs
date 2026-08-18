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
    public class SubFieldController : ControllerBase
    {
        private readonly ISubFieldRepository _repository;
        private readonly IMajorFieldRepository _majorFieldRepository;
        private readonly IMapper _mapper;

        public SubFieldController(
            ISubFieldRepository repository,
            IMajorFieldRepository majorFieldRepository,
            IMapper mapper)
        {
            _repository = repository;
            _majorFieldRepository = majorFieldRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? majorFieldId = null)
        {
            if (majorFieldId.HasValue &&
                majorFieldId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "majorFieldId must be greater than zero."
                });
            }

            if (majorFieldId.HasValue &&
                !await _majorFieldRepository.ExistsAsync(
                    x => x.MajorFieldId == majorFieldId.Value))
            {
                return NotFound(new
                {
                    Message = "Major field not found."
                });
            }

            var items =
                await _repository.GetAllWithMajorFieldAsync(
                    majorFieldId);

            var response =
                _mapper.Map<IEnumerable<SubFieldResponse>>(items);

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] SubFieldCreateRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    Message = "Sub-field name is required."
                });
            }

            if (!request.MajorFieldId.HasValue ||
                request.MajorFieldId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message = "MajorFieldId is required."
                });
            }

            var majorFieldExists =
                await _majorFieldRepository.ExistsAsync(
                    x =>
                        x.MajorFieldId ==
                        request.MajorFieldId.Value);

            if (!majorFieldExists)
            {
                return BadRequest(new
                {
                    Message =
                        "The specified major field does not exist."
                });
            }

            var normalizedName =
                request.Name.Trim();

            var duplicate =
                await _repository.ExistsAsync(
                    x =>
                        x.MajorFieldId ==
                            request.MajorFieldId.Value &&
                        x.Name == normalizedName);

            if (duplicate)
            {
                return Conflict(new
                {
                    Message =
                        "A sub-field with the same name already exists under this major field."
                });
            }

            var item =
                _mapper.Map<SubField>(request);

            item.Name = normalizedName;
            item.CreatedAt ??= DateTime.UtcNow;

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created =
                await _repository.GetByIdWithMajorFieldAsync(
                    item.SubFieldId);

            var response =
                _mapper.Map<SubFieldResponse>(created);

            return CreatedAtAction(
                nameof(GetById),
                new { id = item.SubFieldId },
                response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item =
                await _repository.GetByIdWithMajorFieldAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var response =
                _mapper.Map<SubFieldResponse>(item);

            return Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] SubFieldUpdateRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    Message = "Sub-field name is required."
                });
            }

            if (!request.MajorFieldId.HasValue ||
                request.MajorFieldId.Value <= 0)
            {
                return BadRequest(new
                {
                    Message = "MajorFieldId is required."
                });
            }

            var item =
                await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var majorFieldExists =
                await _majorFieldRepository.ExistsAsync(
                    x =>
                        x.MajorFieldId ==
                        request.MajorFieldId.Value);

            if (!majorFieldExists)
            {
                return BadRequest(new
                {
                    Message =
                        "The specified major field does not exist."
                });
            }

            if (item.MajorFieldId !=
                    request.MajorFieldId.Value &&
                await _repository.HasUsageAsync(id))
            {
                return Conflict(new
                {
                    Message =
                        "The sub-field cannot be moved to another major field while it is referenced by professional profiles, papers, or learning materials."
                });
            }

            var normalizedName =
                request.Name.Trim();

            var duplicate =
                await _repository.ExistsAsync(
                    x =>
                        x.SubFieldId != id &&
                        x.MajorFieldId ==
                            request.MajorFieldId.Value &&
                        x.Name == normalizedName);

            if (duplicate)
            {
                return Conflict(new
                {
                    Message =
                        "A sub-field with the same name already exists under this major field."
                });
            }

            _mapper.Map(request, item);
            item.Name = normalizedName;

            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated =
                await _repository.GetByIdWithMajorFieldAsync(id);

            var response =
                _mapper.Map<SubFieldResponse>(updated);

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

            if (await _repository.HasUsageAsync(id))
            {
                return Conflict(new
                {
                    Message =
                        "The sub-field cannot be deleted because it is referenced by professional profiles, papers, or learning materials."
                });
            }

            _repository.Delete(item);
            await _repository.SaveChangesAsync();

            return NoContent();
        }
    }
}