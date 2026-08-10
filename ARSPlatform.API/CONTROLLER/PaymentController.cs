using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using ARSPlatform.SERVICE;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Tạo payment link PayOS
        /// </summary>
        [HttpPost("create-link")]
        public async Task<IActionResult> CreatePaymentLink([FromBody] PaymentCreateRequest request)
        {
            try
            {
                var result = await _paymentService.CreatePaymentLink(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xử lý callback từ PayOS - Cập nhật DB và redirect về FE
        /// </summary>
        [HttpGet("success")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentSuccess(
            [FromQuery] string? orderCode,
            [FromQuery] string? status,
            [FromQuery] string? code)
        {
            try
            {
                var order = orderCode ?? code;
                if (string.IsNullOrEmpty(order))
                {
                    return BadRequest(new { Message = "Missing orderCode" });
                }

                var paymentStatus = status ?? "SUCCESS";
                var result = await _paymentService.ProcessCallback(order, paymentStatus);
                
                // Redirect về FE
                if (!string.IsNullOrEmpty(result.RedirectUrl))
                {
                    return Redirect(result.RedirectUrl);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xử lý thanh toán thất bại - redirect từ PayOS
        /// </summary>
        [HttpGet("cancel")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCancel([FromQuery] string? orderCode)
        {
            try
            {
                if (!string.IsNullOrEmpty(orderCode))
                {
                    await _paymentService.CancelPayment(orderCode);
                }
                
                // Redirect về FE với thông báo thất bại
                var cancelUrl = $"{Request.Scheme}://{Request.Host}/payment/failed?orderCode={orderCode}";
                return Redirect(cancelUrl);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Hủy payment
        /// </summary>
        [HttpPost("cancel/{orderCode}")]
        public async Task<IActionResult> CancelPayment(string orderCode)
        {
            try
            {
                var result = await _paymentService.CancelPayment(orderCode);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
