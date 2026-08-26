using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<Role>> GetAllAsync();
    }
}
