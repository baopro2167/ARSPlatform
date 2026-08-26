using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARSPlatform.REPOSITORIES
{
    public class ForumPostRepository
        : GenericRepository<ForumPost>,
          IForumPostRepository
    {
        public ForumPostRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<IEnumerable<ForumPost>> SearchAsync(
            string? search,
            string? category,
            string? sort)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.ForumComments)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(p =>
                    (p.Title != null &&
                     p.Title.Contains(keyword))
                    ||
                    p.Content.Contains(keyword)
                    ||
                    (p.Abstract != null &&
                     p.Abstract.Contains(keyword))
                    ||
                    (p.Tags != null &&
                     p.Tags.Contains(keyword))
                    ||
                    (p.User != null &&
                     p.User.FullName.Contains(keyword)));
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                var normalizedCategory = category.Trim();

                query = query.Where(
                    p => p.Category == normalizedCategory);
            }

            // Sort
            switch ((sort ?? "latest").Trim().ToLowerInvariant())
            {
                case "oldest":
                    query = query
                        .OrderBy(p => p.CreatedAt);
                    break;

                case "popular":
                case "likes":
                case "most-liked":
                    query = query
                        .OrderByDescending(p => p.LikeCount)
                        .ThenByDescending(p => p.CreatedAt);
                    break;

                case "views":
                case "most-viewed":
                    query = query
                        .OrderByDescending(p => p.ViewCount)
                        .ThenByDescending(p => p.CreatedAt);
                    break;

                case "latest":
                default:
                    query = query
                        .OrderByDescending(p => p.CreatedAt);
                    break;
            }

            return await query.ToListAsync();
        }

        public async Task<PagedResult<ForumPost>> SearchPagedAsync(
            PaginationParams paginationParams,
            string? search,
            string? category,
            string? sort)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.ForumComments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(p =>
                    (p.Title != null && p.Title.Contains(keyword))
                    || p.Content.Contains(keyword)
                    || (p.Abstract != null && p.Abstract.Contains(keyword))
                    || (p.Tags != null && p.Tags.Contains(keyword))
                    || (p.User != null && p.User.FullName.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var normalizedCategory = category.Trim();
                query = query.Where(p => p.Category == normalizedCategory);
            }

            switch ((sort ?? "latest").Trim().ToLowerInvariant())
            {
                case "oldest":
                    query = query.OrderBy(p => p.CreatedAt);
                    break;
                case "popular":
                case "likes":
                case "most-liked":
                    query = query.OrderByDescending(p => p.LikeCount).ThenByDescending(p => p.CreatedAt);
                    break;
                case "views":
                case "most-viewed":
                    query = query.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.CreatedAt);
                    break;
                case "latest":
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            var totalCount = await query.CountAsync();
            var pageNumber = paginationParams.PageNumber < 1 ? 1 : paginationParams.PageNumber;
            var pageSize = paginationParams.PageSize < 1 ? 10 : paginationParams.PageSize;

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ForumPost>(items, totalCount, pageNumber, pageSize);
        }
    }
}