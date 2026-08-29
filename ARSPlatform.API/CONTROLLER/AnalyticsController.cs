using System;
using System.Threading;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _service;

        public AnalyticsController(IAnalyticsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy số liệu tổng quan hệ thống (Tổng số thành viên, tổng số bài báo)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dữ liệu tổng quan</returns>
        [HttpGet("summary")]
        public async Task<ActionResult<AnalyticsSummaryResponse>> GetSummary(CancellationToken cancellationToken)
        {
            var response = await _service.GetSummaryAsync(cancellationToken);
            return Ok(response);
        }

        /// <summary>
        /// Lấy dữ liệu chuỗi thời gian theo khoảng thời gian và chỉ số (đăng ký người dùng, doanh thu)
        /// </summary>
        /// <param name="range">Khoảng thời gian: daily, weekly, monthly, yearly</param>
        /// <param name="metric">Chỉ số phân tích: user_registrations, revenue</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dữ liệu chuỗi thời gian</returns>
        [HttpGet("timeseries")]
        public async Task<ActionResult<AnalyticsTimeseriesResponse>> GetTimeseries(
            [FromQuery] string? range,
            [FromQuery] string? metric,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.GetTimeseriesAsync(range ?? "", metric ?? "", cancellationToken);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}