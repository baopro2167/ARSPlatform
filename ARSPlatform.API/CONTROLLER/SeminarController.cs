using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using AutoMapper;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SeminarController : ControllerBase
    {
        private readonly ISeminarRepository _repository;
        private readonly IMapper _mapper;
        private readonly IAudioSummaryService _audioSummaryService;

        public SeminarController(ISeminarRepository repository, IMapper mapper, IAudioSummaryService audioSummaryService)
        {
            _repository = repository;
            _mapper = mapper;
            _audioSummaryService = audioSummaryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllAsync();
            var response = _mapper.Map<IEnumerable<SeminarResponse>>(items);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SeminarCreateRequest request)
        {
            var item = _mapper.Map<Seminar>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            var response = _mapper.Map<SeminarResponse>(item);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            var response = _mapper.Map<SeminarResponse>(item);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SeminarUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            var response = _mapper.Map<SeminarResponse>(item);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            _repository.Delete(item);
            await _repository.SaveChangesAsync();
            return Ok(new { Message = "Deleted successfully." });
        }

        [HttpPost("{id:int}/summarize-audio")]
        [RequestSizeLimit(524_288_000)] // Cho phép file tới 500 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
        [ProducesResponseType(typeof(SeminarAudioSummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SummarizeAudio(int id, [FromForm] SeminarAudioSummaryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _audioSummaryService.SummarizeSeminarAudioAsync(id, request, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
