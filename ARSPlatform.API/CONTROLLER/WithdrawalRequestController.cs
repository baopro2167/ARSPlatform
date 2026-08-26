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
    public class WithdrawalRequestController : ControllerBase
    {
        private readonly IWithdrawalRequestService _service;

        public WithdrawalRequestController(IWithdrawalRequestService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ yêu cầu rút tiền
        /// </summary>
        /// <returns>Danh sách yêu cầu rút tiền</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WithdrawalRequestResponse>>> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        /// <summary>
        /// Tạo mới yêu cầu rút tiền
        /// </summary>
        /// <param name="request">Thông tin yêu cầu rút tiền</param>
        /// <returns>Yêu cầu rút tiền vừa tạo</returns>
        [HttpPost]
        public async Task<ActionResult<WithdrawalRequestResponse>> Create([FromBody] WithdrawalRequestCreateRequest request)
        {
            var response = await _service.CreateAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết yêu cầu rút tiền theo ID
        /// </summary>
        /// <param name="id">ID yêu cầu rút tiền</param>
        /// <returns>Chi tiết yêu cầu rút tiền</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<WithdrawalRequestResponse>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { Message = "Withdrawal request not found." });
            return Ok(item);
        }
    }
}