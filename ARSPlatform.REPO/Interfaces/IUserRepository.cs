using ARSPlatform.MODEL.Entities;
using System;
using System.Threading.Tasks;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByOrcidAsync(string orcidId);

        Task<User?> GetWithRoleByIdAsync(int id);
        Task<int> CountAllAsync(System.Threading.CancellationToken cancellationToken = default);
        Task<System.Collections.Generic.List<System.DateTime>> GetRegistrationDatesAsync(System.Threading.CancellationToken cancellationToken = default);
    }
}
