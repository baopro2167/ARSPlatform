using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
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
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CommentVoteResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// LẤY DANH SÁCH THEO (ID) CỦA TỪNG CONTROLLER , TRUYỀN VÀO PAGESIZE VÀ PAGENUMBER LÀ SẼ LIST LÊN DANH SÁCH CÓ PHÂN TRANG 
        /// </summary>
        /// <param name="paginationParams">Tham số phân trang (PageNumber, PageSize)</param>
        /// <returns>Danh sách lượt bình chọn có phân trang</returns>
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<CommentVoteResponse>>> GetPaged([FromQuery] PaginationParams paginationParams)
        {
            var result = await _service.GetPagedAsync(paginationParams);
            return Ok(result);
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
