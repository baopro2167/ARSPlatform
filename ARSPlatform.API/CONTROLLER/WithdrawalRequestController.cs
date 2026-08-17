using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class WithdrawalRequestController : ControllerBase
    {
        private readonly IWithdrawalRequestRepository _repository;
        private readonly IMapper _mapper;

        public WithdrawalRequestController(
            IWithdrawalRequestRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllAsync();

            var response =
                _mapper.Map<IEnumerable<WithdrawalRequestResponse>>(items);

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] WithdrawalRequestCreateRequest request)
        {
            var item = _mapper.Map<WithdrawalRequest>(request);

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var response =
                _mapper.Map<WithdrawalRequestResponse>(item);

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var response =
                _mapper.Map<WithdrawalRequestResponse>(item);

            return Ok(response);
        }
    }
}