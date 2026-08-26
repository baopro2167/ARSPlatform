using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IReportService
    {
        Task<IEnumerable<ReportResponse>> GetAllAsync();
        Task<ReportResponse?> GetByIdAsync(int id);
        Task<ReportResponse> CreateAsync(ReportCreateRequest request);
        Task<ReportResponse?> UpdateAsync(int id, ReportUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
