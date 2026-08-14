using ARSPlatform.MODEL.Entities;
using System;
using System.Threading.Tasks;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetWithRoleByIdAsync(int id);
    }
}
