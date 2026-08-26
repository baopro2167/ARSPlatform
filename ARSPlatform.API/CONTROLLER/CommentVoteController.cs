using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommentVoteController : ControllerBase
    {
        private readonly ICommentVoteService _service;

        public CommentVoteController(ICommentVoteService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách lượt bình chọn / đánh giá cho các bình luận
        /// </summary>
        /// <returns>Danh sách bình chọn</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommentVoteResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Bình chọn hoặc thay đổi đánh giá (upvote/downvote) cho bình luận
        /// </summary>
        /// <param name="request">Thông tin bình chọn</param>
        /// <returns>Bản ghi bình chọn</returns>
        [HttpPost]
        public async Task<ActionResult<CommentVoteResponse>> Create([FromBody] CommentVoteCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }
    }
}
