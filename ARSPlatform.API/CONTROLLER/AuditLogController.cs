using System.Globalization;
using System.Security.Claims;
using System.Text;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARSPlatform.API.CONTROLLER
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogRepository _repository;
        private readonly IMapper _mapper;

        public AuditLogController(
            IAuditLogRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] int? adminId = null,
            [FromQuery] string? range = "all_time",
            [FromQuery] PaginationParams? paginationParams = null)
        {
            try
            {
                paginationParams ??= new PaginationParams();

                var result = await _repository.GetPagedAsync(
                    search,
                    adminId,
                    range,
                    paginationParams);

                var response = new PagedResult<AuditLogResponse>
                {
                    Items = _mapper.Map<List<AuditLogResponse>>(result.Items),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] string? search = null,
            [FromQuery] int? adminId = null,
            [FromQuery] string? range = "all_time")
        {
            try
            {
                var logs = await _repository.GetForExportAsync(
                    search,
                    adminId,
                    range);

                var csv = new StringBuilder();

                csv.AppendLine(
                    "LOG_ID,TIMESTAMP,ADMIN_ID,ADMIN_NAME,ACTION,TARGET_ID,TARGET,DETAILS");

                foreach (var log in logs)
                {
                    csv.AppendLine(string.Join(",",
                        EscapeCsv(log.LogId.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsv(log.Timestamp.ToString("O", CultureInfo.InvariantCulture)),
                        EscapeCsv(log.AdminId.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsv(log.AdminName),
                        EscapeCsv(log.Action),
                        EscapeCsv(log.TargetId),
                        EscapeCsv(log.Target),
                        EscapeCsv(log.Details)));
                }

                var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                var bytes = utf8Bom.GetBytes(csv.ToString());

                return File(
                    bytes,
                    "text/csv; charset=utf-8",
                    $"audit-log-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] AuditLogCreateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    Message = "Request body is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.AdminName))
            {
                return BadRequest(new
                {
                    Message = "AdminName is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Action))
            {
                return BadRequest(new
                {
                    Message = "Action is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Target))
            {
                return BadRequest(new
                {
                    Message = "Target is required."
                });
            }

            var currentAdminIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(currentAdminIdClaim, out var currentAdminId) &&
                currentAdminId != request.AdminId)
            {
                return Forbid();
            }

            var entity = _mapper.Map<AuditLog>(request);

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var response = _mapper.Map<AuditLogResponse>(entity);

            return CreatedAtAction(
                nameof(GetAll),
                new { },
                response);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");

            if (escaped.Contains(',') ||
                escaped.Contains('"') ||
                escaped.Contains('\r') ||
                escaped.Contains('\n'))
            {
                return $"\"{escaped}\"";
            }

            return escaped;
        }
    }
}