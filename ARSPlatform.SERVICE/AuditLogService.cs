using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.SERVICES
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _repository;
        private readonly IMapper _mapper;

        public AuditLogService(IAuditLogRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<AuditLogResponse>> GetPagedAsync(
            string? search,
            int? adminId,
            string? range,
            PaginationParams? paginationParams)
        {
            paginationParams ??= new PaginationParams();
            var result = await _repository.GetPagedAsync(search, adminId, range, paginationParams);

            return new PagedResult<AuditLogResponse>
            {
                Items = _mapper.Map<List<AuditLogResponse>>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }

        public async Task<byte[]> ExportCsvAsync(string? search, int? adminId, string? range)
        {
            var logs = await _repository.GetForExportAsync(search, adminId, range);

            var csv = new StringBuilder();
            csv.AppendLine("LOG_ID,TIMESTAMP,ADMIN_ID,ADMIN_NAME,ACTION,TARGET_ID,TARGET,DETAILS");

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
            return utf8Bom.GetBytes(csv.ToString());
        }

        public async Task<AuditLogResponse> CreateAsync(AuditLogCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AdminName))
            {
                throw new ArgumentException("AdminName is required.");
            }
            if (string.IsNullOrWhiteSpace(request.Action))
            {
                throw new ArgumentException("Action is required.");
            }
            if (string.IsNullOrWhiteSpace(request.Target))
            {
                throw new ArgumentException("Target is required.");
            }

            var entity = _mapper.Map<AuditLog>(request);
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<AuditLogResponse>(entity);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var escaped = value.Replace("\"", "\"\"");
            if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\r') || escaped.Contains('\n'))
            {
                return $"\"{escaped}\"";
            }
            return escaped;
        }
    }
}
