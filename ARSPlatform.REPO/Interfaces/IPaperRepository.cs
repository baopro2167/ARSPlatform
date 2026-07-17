using ARSPlatform.MODEL.Entities;
using System;
using System.Threading.Tasks;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IPaperRepository : IGenericRepository<Paper>
    {
        Task<Paper?> GetWithAuthorByIdAsync(int id);
    }
}
