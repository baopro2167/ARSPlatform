using System;
using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.SERVICE;

public class UserRewardService : IUserRewardService
{
    private readonly IUserRewardRepository _repo;

    public UserRewardService(IUserRewardRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<UserRewardResponse>> GetAllPagedAsync(UserRewardFilterParams filter)
    {
        var page = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var size = filter.PageSize < 1 ? 10 : filter.PageSize;

        var query = _repo.GetQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(r => r.Name.Contains(filter.Search));

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(r => r.Status == filter.Status);

        // Sort
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDir == "asc"
                ? query.OrderBy(r => r.Name)
                : query.OrderByDescending(r => r.Name),
            "updatedat" => filter.SortDir == "asc"
                ? query.OrderBy(r => r.UpdateAt)
                : query.OrderByDescending(r => r.UpdateAt),
            "createdat" => filter.SortDir == "asc"
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PagedResult<UserRewardResponse>(
            items.Select(MapToResponse).ToList(),
            total, page, size);
    }

    public async Task<UserRewardResponse?> GetByIdAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity == null ? null : MapToResponse(entity);
    }

    public async Task<UserRewardResponse> CreateAsync(UserRewardCreateRequest request)
    {
        // Validate Status
        if (request.Status != "Active" && request.Status != "InActive")
            throw new ArgumentException("Status must be 'Active' or 'InActive'");

        var entity = new UserReward
        {
            Name = request.Name,
            Description = request.Description,
            RewardMonths = request.RewardMonths,
            UpdateAt = request.UpdateAt,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<UserRewardResponse?> UpdateAsync(int id, UserRewardUpdateRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return null;

        // Validate Status
        if (request.Status != "Active" && request.Status != "InActive")
            throw new ArgumentException("Status must be 'Active' or 'InActive'");

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.RewardMonths = request.RewardMonths;
        entity.UpdateAt = request.UpdateAt;
        entity.Status = request.Status;

        _repo.Update(entity);
        await _repo.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;

        _repo.Delete(entity);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<UserRewardResponse?> ToggleAsync(int id, bool isActive)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return null;

        entity.Status = isActive ? "Active" : "InActive";
        entity.UpdateAt = DateTime.UtcNow;

        _repo.Update(entity);
        await _repo.SaveChangesAsync();

        return MapToResponse(entity);
    }

    private static UserRewardResponse MapToResponse(UserReward entity)
    {
        return new UserRewardResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            RewardMonths = entity.RewardMonths,
            UpdateAt = entity.UpdateAt,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
    }
}
