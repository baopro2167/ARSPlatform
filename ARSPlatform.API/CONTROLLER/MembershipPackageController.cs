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
    public class MembershipPackageController : ControllerBase
    {
        private readonly IMembershipPackageRepository _repository;
        private readonly IMapper _mapper;

        public MembershipPackageController(IMembershipPackageRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllAsync();
            var response = _mapper.Map<IEnumerable<MembershipPackageResponse>>(items);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MembershipPackageCreateRequest request)
        {
            var item = _mapper.Map<MembershipPackage>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            var response = _mapper.Map<MembershipPackageResponse>(item);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            var response = _mapper.Map<MembershipPackageResponse>(item);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] MembershipPackageUpdateRequest request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            _mapper.Map(request, item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            var response = _mapper.Map<MembershipPackageResponse>(item);
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
    }
}
