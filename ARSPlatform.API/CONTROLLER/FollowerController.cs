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
    public class FollowerController : ControllerBase
    {
        private readonly IFollowerService _service;

        public FollowerController(IFollowerService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách theo dõi giữa các nhà nghiên cứu / người dùng
        /// </summary>
        /// <returns>Danh sách quan hệ theo dõi</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FollowerResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Theo dõi một người dùng / tác giả khác
        /// </summary>
        /// <param name="request">Thông tin theo dõi</param>
        /// <returns>Bản ghi theo dõi vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<FollowerResponse>> Create([FromBody] FollowerCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }
    }
}
