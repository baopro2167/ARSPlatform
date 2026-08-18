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
    public class ReviewRequestController : ControllerBase
    {
        private readonly IReviewRequestRepository _repository;
        private readonly IMapper _mapper;

        public ReviewRequestController(IReviewRequestRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllWithReviewerAsync();
            var response = _mapper.Map<IEnumerable<ReviewRequestResponse>>(items);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReviewRequestCreateRequest request)
        {
            var item = _mapper.Map<ReviewRequest>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdWithReviewerAsync(item.ReviewRequestId);
            var response = _mapper.Map<ReviewRequestResponse>(created);
            return CreatedAtAction(nameof(GetById), new { id = item.ReviewRequestId }, response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository.GetByIdWithReviewerAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            var response = _mapper.Map<ReviewRequestResponse>(item);
            return Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ReviewRequestUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdWithReviewerAsync(id);
            var response = _mapper.Map<ReviewRequestResponse>(updated);
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