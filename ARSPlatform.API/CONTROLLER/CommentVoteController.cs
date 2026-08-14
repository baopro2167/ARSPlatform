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
    public class CommentVoteController : ControllerBase
    {
        private readonly ICommentVoteRepository _repository;
        private readonly IMapper _mapper;

        public CommentVoteController(ICommentVoteRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllAsync();
            var response = _mapper.Map<IEnumerable<CommentVoteResponse>>(items);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CommentVoteCreateRequest request)
        {
            var item = _mapper.Map<CommentVote>(request);
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            var response = _mapper.Map<CommentVoteResponse>(item);
            return Ok(response);
        }
    }
}
