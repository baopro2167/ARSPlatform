using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<Notification>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.UserId == userId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<Notification, object>>[]
                {
                    x => x.User!
                });
        }

        public async Task<PagedResult<Notification>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize)
        {
            return await GetByUserIdPagedAsync(userId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _dbSet
                .CountAsync(x => x.UserId == userId && (x.IsRead == null || x.IsRead == false));
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _dbSet
                .Where(x => x.UserId == userId && (x.IsRead == null || x.IsRead == false))
                .ToListAsync();

            if (!unreadNotifications.Any())
                return 0;

            foreach (var item in unreadNotifications)
            {
                item.IsRead = true;
            }

            return await _context.SaveChangesAsync();
        }
    }
}
