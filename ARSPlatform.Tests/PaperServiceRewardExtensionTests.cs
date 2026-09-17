using System;
using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using ARSPlatform.SERVICES;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ARSPlatform.Tests;

/// <summary>
/// End-to-end tests for the Researcher time-extension + notification
/// side-effect that fires when a Paper is set to <c>Approved</c>.
///
/// Uses EF Core InMemory to spin up a fresh <see cref="AppDbContext"/>;
/// the public <see cref="PaperService.UpdatePaperForTestingAsync"/>
/// path (the dedicated TEST API on the controller) is invoked so the
/// OpenAlex / ORCID guards are bypassed and we can focus on the
/// reward extension logic itself.
/// </summary>
public class PaperServiceRewardExtensionTests
{
    private const string ResearcherRole = "Researcher";
    private const string Approved = "Approved";

    private static AppDbContext NewContext(string dbName) =>
        new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options);

    private static PaperService NewPaperService(
        AppDbContext context,
        out Mock<IPaperRepository> paperRepoMock,
        out Mock<INotificationService> notificationMock)
    {
        paperRepoMock = new Mock<IPaperRepository>();
        notificationMock = new Mock<INotificationService>();

        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<PaperResponse>(It.IsAny<Paper>()))
            .Returns((Paper p) => new PaperResponse
            {
                Id = p.PaperId,
                Title = p.Title,
                Abstract = p.Abstract ?? string.Empty,
                Status = p.Status,
                AuthorId = p.CreatorId,
                PaperType = p.PaperType
            });

        var externalApi = new Mock<IExternalApiService>();
        var openAlex = new Mock<IOpenAlexService>();

        return new PaperService(
            paperRepoMock.Object,
            externalApi.Object,
            openAlex.Object,
            mapperMock.Object,
            context,
            notificationMock.Object);
    }

    private static PaperUpdateRequest NewUpdateRequest(
        string status, string title = "Quantum AI Paper")
    {
        return new PaperUpdateRequest
        {
            Title = title,
            Abstract = "An abstract",
            FileUrl = "https://example.com/paper.pdf",
            Status = status,
            Issn = false,
            IsOpenAccess = true,
            Quartile = "Q1",
            SubFieldId = 1,
            PaperType = "Journal"
        };
    }

    private static Paper NewPaper(int paperId, int creatorId)
    {
        return new Paper
        {
            PaperId = paperId,
            CreatorId = creatorId,
            Title = "Quantum AI Paper",
            Abstract = "An abstract",
            FileUrl = "https://example.com/paper.pdf",
            Status = "Submitted",
            PaperType = "Journal",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // 1) Reward name matching — case / underscore / hyphen / spaces
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Research Publication Reward")]
    [InlineData("research_publication_reward")]
    [InlineData("RESEARCH_PUBLICATION_REWARD")]
    [InlineData("researchpublicationreward")]
    [InlineData("ResearchPublicationReward")]
    [InlineData("research-publication-reward")]
    [InlineData("Research  Publication   Reward")]
    [InlineData("  research_publication_reward  ")]
    public async Task ApprovePaper_WithVariousNameFormats_StillExtendsSubscriptionAndNotifies(
        string rewardNameInDb)
    {
        // Arrange — each row gets a unique DB name to avoid cross-test pollution.
        var dbName = nameof(ApprovePaper_WithVariousNameFormats_StillExtendsSubscriptionAndNotifies) +
                     "_" + Guid.NewGuid().ToString("N");
        await using var context = NewContext(dbName);

        var creatorId = 42;
        var paper = NewPaper(paperId: 7, creatorId: creatorId);
        context.Papers.Add(paper);

        context.UserRewards.Add(new UserReward
        {
            Name = rewardNameInDb,
            Description = "Reward for publishing",
            RewardMonths = 3,
            Status = "Active",
            UpdateAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        // Pre-existing subscription that expires in 10 days from now —
        // extension should add 3 months on top of baseExpires.
        var baseExpires = DateTime.UtcNow.AddDays(10);
        context.UserSubscriptions.Add(new UserSubscription
        {
            UserId = creatorId,
            UserRole = ResearcherRole,
            ExpiresAt = baseExpires,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30)
        });

        await context.SaveChangesAsync();

        var service = NewPaperService(
            context,
            out var paperRepoMock,
            out _);

        paperRepoMock
            .Setup(r => r.GetWithAuthorByIdAsync(paper.PaperId))
            .ReturnsAsync(paper);

        // Act
        var result = await service.UpdatePaperForTestingAsync(
            paper.PaperId,
            NewUpdateRequest(Approved, paper.Title));

        // Assert
        Assert.NotNull(result);

        var subscription = await context.UserSubscriptions
            .AsNoTracking()
            .FirstAsync(s => s.UserId == creatorId && s.UserRole == ResearcherRole);

        Assert.NotNull(subscription.ExpiresAt);
        Assert.True(
            subscription.ExpiresAt > baseExpires,
            $"ExpiresAt should be extended past {baseExpires:o} but is {subscription.ExpiresAt:o}");

        // 3 months added on top of baseExpires — allow ±1 second for clock skew.
        var expected = baseExpires.AddMonths(3);
        Assert.True(
            Math.Abs((subscription.ExpiresAt!.Value - expected).TotalSeconds) < 1.0,
            $"Expected ~{expected:o} but got {subscription.ExpiresAt:o}");

        // Notification should be sent.
        var notifications = await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == creatorId)
            .ToListAsync();

        Assert.Single(notifications);
        Assert.Equal(
            $"Bạn đã được gia hạn thời gian sử dụng vai trò này khi đăng \"{paper.Title}\" đã được đăng lên hệ thống",
            notifications[0].Message);
        Assert.False(notifications[0].IsRead);
    }

    [Fact]
    public async Task ApprovePaper_WithDifferentRewardName_DoesNotExtendAndDoesNotNotify()
    {
        // Arrange — name does NOT match "Research Publication Reward".
        var dbName = nameof(ApprovePaper_WithDifferentRewardName_DoesNotExtendAndDoesNotNotify) +
                     "_" + Guid.NewGuid().ToString("N");
        await using var context = NewContext(dbName);

        var creatorId = 99;
        var paper = NewPaper(paperId: 11, creatorId: creatorId);
        context.Papers.Add(paper);

        context.UserRewards.Add(new UserReward
        {
            Name = "Lecture Reward",
            RewardMonths = 6,
            Status = "Active",
            UpdateAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        var baseExpires = DateTime.UtcNow.AddDays(15);
        context.UserSubscriptions.Add(new UserSubscription
        {
            UserId = creatorId,
            UserRole = ResearcherRole,
            ExpiresAt = baseExpires,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = NewPaperService(
            context,
            out var paperRepoMock,
            out _);

        paperRepoMock
            .Setup(r => r.GetWithAuthorByIdAsync(paper.PaperId))
            .ReturnsAsync(paper);

        // Act
        var result = await service.UpdatePaperForTestingAsync(
            paper.PaperId,
            NewUpdateRequest(Approved, paper.Title));

        // Assert
        Assert.NotNull(result);

        var subscription = await context.UserSubscriptions
            .AsNoTracking()
            .FirstAsync(s => s.UserId == creatorId && s.UserRole == ResearcherRole);

        Assert.True(
            Math.Abs((subscription.ExpiresAt!.Value - baseExpires).TotalSeconds) < 1.0,
            "ExpiresAt must remain unchanged when reward name does not match.");

        var notifications = await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == creatorId)
            .ToListAsync();
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task ApprovePaper_WithNoSubscriptionYet_CreatesOneAndNotifies()
    {
        // Arrange
        var dbName = nameof(ApprovePaper_WithNoSubscriptionYet_CreatesOneAndNotifies) +
                     "_" + Guid.NewGuid().ToString("N");
        await using var context = NewContext(dbName);

        var creatorId = 7;
        var paper = NewPaper(paperId: 23, creatorId: creatorId);
        context.Papers.Add(paper);

        context.UserRewards.Add(new UserReward
        {
            Name = "research_publication_reward", // underscore variant
            RewardMonths = 2,
            Status = "Active",
            UpdateAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = NewPaperService(
            context,
            out var paperRepoMock,
            out _);

        paperRepoMock
            .Setup(r => r.GetWithAuthorByIdAsync(paper.PaperId))
            .ReturnsAsync(paper);

        // Act
        var before = DateTime.UtcNow;
        var result = await service.UpdatePaperForTestingAsync(
            paper.PaperId,
            NewUpdateRequest(Approved, paper.Title));
        var after = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result);

        var subscription = await context.UserSubscriptions
            .AsNoTracking()
            .FirstAsync(s => s.UserId == creatorId && s.UserRole == ResearcherRole);

        Assert.NotNull(subscription.ExpiresAt);

        // ExpiresAt should be roughly `before + 2 months`.
        var minExpected = before.AddMonths(2).AddMinutes(-1);
        var maxExpected = after.AddMonths(2).AddMinutes(1);
        Assert.InRange(subscription.ExpiresAt!.Value, minExpected, maxExpected);

        var notifications = await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == creatorId)
            .ToListAsync();
        Assert.Single(notifications);
    }

    [Fact]
    public async Task ApprovePaper_WithInactiveReward_DoesNothing()
    {
        // Arrange
        var dbName = nameof(ApprovePaper_WithInactiveReward_DoesNothing) +
                     "_" + Guid.NewGuid().ToString("N");
        await using var context = NewContext(dbName);

        var creatorId = 5;
        var paper = NewPaper(paperId: 31, creatorId: creatorId);
        context.Papers.Add(paper);

        context.UserRewards.Add(new UserReward
        {
            Name = "Research Publication Reward",
            RewardMonths = 5,
            Status = "InActive",
            UpdateAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        var baseExpires = DateTime.UtcNow.AddDays(20);
        context.UserSubscriptions.Add(new UserSubscription
        {
            UserId = creatorId,
            UserRole = ResearcherRole,
            ExpiresAt = baseExpires,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = NewPaperService(
            context,
            out var paperRepoMock,
            out _);

        paperRepoMock
            .Setup(r => r.GetWithAuthorByIdAsync(paper.PaperId))
            .ReturnsAsync(paper);

        // Act
        await service.UpdatePaperForTestingAsync(
            paper.PaperId,
            NewUpdateRequest(Approved, paper.Title));

        // Assert
        var subscription = await context.UserSubscriptions
            .AsNoTracking()
            .FirstAsync(s => s.UserId == creatorId && s.UserRole == ResearcherRole);

        Assert.True(
            Math.Abs((subscription.ExpiresAt!.Value - baseExpires).TotalSeconds) < 1.0,
            "Inactive reward must not extend the subscription.");

        var notifications = await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == creatorId)
            .ToListAsync();
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task ApprovePaper_WithExpiredSubscription_ExtendsFromNow()
    {
        // Arrange — subscription has already expired.
        var dbName = nameof(ApprovePaper_WithExpiredSubscription_ExtendsFromNow) +
                     "_" + Guid.NewGuid().ToString("N");
        await using var context = NewContext(dbName);

        var creatorId = 8;
        var paper = NewPaper(paperId: 47, creatorId: creatorId);
        context.Papers.Add(paper);

        context.UserRewards.Add(new UserReward
        {
            Name = "Research Publication Reward",
            RewardMonths = 1,
            Status = "Active",
            UpdateAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        var expired = DateTime.UtcNow.AddDays(-30);
        context.UserSubscriptions.Add(new UserSubscription
        {
            UserId = creatorId,
            UserRole = ResearcherRole,
            ExpiresAt = expired,
            CreatedAt = DateTime.UtcNow.AddDays(-365),
            UpdatedAt = DateTime.UtcNow.AddDays(-100)
        });

        await context.SaveChangesAsync();

        var service = NewPaperService(
            context,
            out var paperRepoMock,
            out _);

        paperRepoMock
            .Setup(r => r.GetWithAuthorByIdAsync(paper.PaperId))
            .ReturnsAsync(paper);

        // Act
        var before = DateTime.UtcNow;
        await service.UpdatePaperForTestingAsync(
            paper.PaperId,
            NewUpdateRequest(Approved, paper.Title));
        var after = DateTime.UtcNow;

        // Assert — should extend from `now`, NOT from the expired date.
        var subscription = await context.UserSubscriptions
            .AsNoTracking()
            .FirstAsync(s => s.UserId == creatorId && s.UserRole == ResearcherRole);

        var minExpected = before.AddMonths(1).AddMinutes(-1);
        var maxExpected = after.AddMonths(1).AddMinutes(1);
        Assert.InRange(subscription.ExpiresAt!.Value, minExpected, maxExpected);
    }

    [Fact]
    public async Task ApprovePaper_NotApproved_DoesNotExtendAndDoesNotNotify()
    {
        // Arrange — status is NOT "Approved".
        var dbName = nameof(ApprovePaper_NotApproved_DoesNotExtendAndDoesNotNotify) +
                     "_" + Guid.NewGuid().ToString("N");
        await using var context = NewContext(dbName);

        var creatorId = 12;
        var paper = NewPaper(paperId: 59, creatorId: creatorId);
        context.Papers.Add(paper);

        context.UserRewards.Add(new UserReward
        {
            Name = "Research Publication Reward",
            RewardMonths = 3,
            Status = "Active",
            UpdateAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        var baseExpires = DateTime.UtcNow.AddDays(7);
        context.UserSubscriptions.Add(new UserSubscription
        {
            UserId = creatorId,
            UserRole = ResearcherRole,
            ExpiresAt = baseExpires,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = NewPaperService(
            context,
            out var paperRepoMock,
            out _);

        paperRepoMock
            .Setup(r => r.GetWithAuthorByIdAsync(paper.PaperId))
            .ReturnsAsync(paper);

        // Act — set Status to "Rejected", NOT Approved.
        await service.UpdatePaperForTestingAsync(
            paper.PaperId,
            NewUpdateRequest("Rejected", paper.Title));

        // Assert
        var subscription = await context.UserSubscriptions
            .AsNoTracking()
            .FirstAsync(s => s.UserId == creatorId && s.UserRole == ResearcherRole);

        Assert.True(
            Math.Abs((subscription.ExpiresAt!.Value - baseExpires).TotalSeconds) < 1.0,
            "Non-Approved status must not extend the subscription.");

        var notifications = await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == creatorId)
            .ToListAsync();
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task ApprovePaper_WithNullCreatorId_DoesNotExtend()
    {
        // Arrange — CreatorId is null (orphan paper). Must skip safely.
        var dbName = nameof(ApprovePaper_WithNullCreatorId_DoesNotExtend) +
                     "_" + Guid.NewGuid().ToString("N");
        await using var context = NewContext(dbName);

        var paper = new Paper
        {
            PaperId = 71,
            CreatorId = null,
            Title = "Orphan Paper",
            Abstract = "abc",
            Status = "Submitted",
            PaperType = "Journal",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Papers.Add(paper);

        context.UserRewards.Add(new UserReward
        {
            Name = "Research Publication Reward",
            RewardMonths = 3,
            Status = "Active",
            UpdateAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = NewPaperService(
            context,
            out var paperRepoMock,
            out _);

        paperRepoMock
            .Setup(r => r.GetWithAuthorByIdAsync(paper.PaperId))
            .ReturnsAsync(paper);

        // Act — should not throw.
        var result = await service.UpdatePaperForTestingAsync(
            paper.PaperId,
            NewUpdateRequest(Approved, paper.Title));

        // Assert
        Assert.NotNull(result);
        var subscriptions = await context.UserSubscriptions.ToListAsync();
        Assert.Empty(subscriptions);
        var notifications = await context.Notifications.ToListAsync();
        Assert.Empty(notifications);
    }
}
