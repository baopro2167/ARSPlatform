using ARSPlatform.MODEL;
using ARSPlatform.SERVICE.DTOs.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.API.CONTROLLER;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private static readonly string[] SupportedRanges =
        ["daily", "weekly", "monthly", "yearly"];

    private static readonly string[] SupportedMetrics =
        ["user_registrations", "revenue"];

    private readonly AppDbContext _dbContext;

    public AnalyticsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AnalyticsSummaryResponse>> GetSummary(
        CancellationToken cancellationToken)
    {
        var response = new AnalyticsSummaryResponse
        {
            TotalMembers = await _dbContext.Users
                .AsNoTracking()
                .CountAsync(cancellationToken),

            TotalPapers = await _dbContext.Papers
                .AsNoTracking()
                .CountAsync(cancellationToken)
        };

        return Ok(response);
    }

    [HttpGet("timeseries")]
    public async Task<ActionResult<AnalyticsTimeseriesResponse>> GetTimeseries(
        [FromQuery] string? range,
        [FromQuery] string? metric,
        CancellationToken cancellationToken)
    {
        range = range?.Trim().ToLowerInvariant();
        metric = metric?.Trim().ToLowerInvariant();

        if (range == null || !SupportedRanges.Contains(range))
        {
            return BadRequest(new
            {
                Message =
                    "range must be one of: daily, weekly, monthly, yearly."
            });
        }

        if (metric == null || !SupportedMetrics.Contains(metric))
        {
            return BadRequest(new
            {
                Message =
                    "metric must be one of: user_registrations, revenue."
            });
        }

        var points = metric switch
        {
            "user_registrations" =>
                await GetUserRegistrationPointsAsync(
                    range,
                    cancellationToken),

            "revenue" =>
                await GetRevenuePointsAsync(
                    range,
                    cancellationToken),

            _ => new List<AnalyticsTimeseriesPointResponse>()
        };

        return Ok(new AnalyticsTimeseriesResponse
        {
            Range = range,
            Metric = metric,
            Points = points
        });
    }

    private async Task<List<AnalyticsTimeseriesPointResponse>>
        GetUserRegistrationPointsAsync(
            string range,
            CancellationToken cancellationToken)
    {
        var dates = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.CreatedAt.HasValue)
            .Select(user => user.CreatedAt!.Value)
            .ToListAsync(cancellationToken);

        return dates
            .GroupBy(date => BucketDate(date, range))
            .OrderBy(group => group.Key)
            .Select(group => new AnalyticsTimeseriesPointResponse
            {
                Date = group.Key,
                Value = group.Count()
            })
            .ToList();
    }

    private async Task<List<AnalyticsTimeseriesPointResponse>>
        GetRevenuePointsAsync(
            string range,
            CancellationToken cancellationToken)
    {
        var purchases = await _dbContext.MembershipPurchases
            .AsNoTracking()
            .Where(purchase => purchase.PurchasedAt.HasValue)
            .Select(purchase => new
            {
                Date = purchase.PurchasedAt!.Value,
                purchase.PricePaid
            })
            .ToListAsync(cancellationToken);

        return purchases
            .GroupBy(purchase => BucketDate(purchase.Date, range))
            .OrderBy(group => group.Key)
            .Select(group => new AnalyticsTimeseriesPointResponse
            {
                Date = group.Key,
                Value = group.Sum(item => item.PricePaid)
            })
            .ToList();
    }

    private static DateTime BucketDate(
        DateTime date,
        string range)
    {
        var utcDate = date.Kind == DateTimeKind.Utc
            ? date
            : DateTime.SpecifyKind(
                date,
                DateTimeKind.Utc);

        return range switch
        {
            "daily" =>
                utcDate.Date,

            "weekly" =>
                StartOfWeek(utcDate.Date),

            "monthly" =>
                new DateTime(
                    utcDate.Year,
                    utcDate.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),

            "yearly" =>
                new DateTime(
                    utcDate.Year,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),

            _ => utcDate.Date
        };
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var daysSinceMonday =
            ((int)date.DayOfWeek + 6) % 7;

        return date
            .AddDays(-daysSinceMonday)
            .Date;
    }
}