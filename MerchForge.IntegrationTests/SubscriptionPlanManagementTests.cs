using FluentAssertions;
using MerchForge.api.Data;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.Audit;
using MerchForge.api.Services.Audit.interfaces;
using MerchForge.api.Services.Common;
using MerchForge.api.Services.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MerchForge.IntegrationTests;

/// <summary>No request context in an integration test - always "no acting user", same as an unauthenticated/system call.</summary>
internal class NullCurrentUserAccessor : ICurrentUserAccessor
{
    public Guid? UserId => null;
}

/// <summary>
/// The Super Admin Plans &amp; Subscriptions enhancement's new aggregation and
/// listing logic: merging Monthly/Yearly plan rows into one tier for the plan
/// cards, platform subscription stats, and the new platform-wide subscriptions
/// list/filters/recent-activity feed. Exercised against the real repositories,
/// matching this suite's established convention (real MariaDB, not a double).
/// </summary>
public class SubscriptionPlanManagementTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _businessA = null!;
    private Business _businessB = null!;
    private Business _businessC = null!;

    public SubscriptionPlanManagementTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _businessA = await _fixture.CreateBusinessAsync("Plan Test Co A", CatalogDatabaseFixture.FashionDomainId);
        _businessB = await _fixture.CreateBusinessAsync("Plan Test Co B", CatalogDatabaseFixture.FashionDomainId);
        _businessC = await _fixture.CreateBusinessAsync("Plan Test Co C", CatalogDatabaseFixture.FashionDomainId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static SubscriptionPlanService CreatePlanService(MerchForgeDbContext db) => new(
        new SubscriptionPlanRepository(db),
        new AuditLogService(new AuditLogRepository(db), NullLogger<AuditLogService>.Instance),
        new NullCurrentUserAccessor());

    private static SubscriptionPlan MakePlan(string name, BillingInterval interval, decimal price, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Price = price,
        Currency = "USD",
        BillingInterval = interval,
        IsActive = isActive,
        IsCustom = true,
    };

    private static Subscription MakeSubscription(
        Guid businessId, Guid planId, SubscriptionStatus status = SubscriptionStatus.Active, DateTime? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        SubscriptionPlanId = planId,
        Status = status,
        CurrentPeriodStart = createdAt ?? DateTime.UtcNow,
        CurrentPeriodEnd = (createdAt ?? DateTime.UtcNow).AddMonths(1),
        CreatedAt = createdAt ?? DateTime.UtcNow,
        UpdatedAt = createdAt ?? DateTime.UtcNow,
    };

    [Fact]
    public async Task Plan_groups_merge_monthly_and_yearly_rows_sharing_a_name_and_sum_subscriber_counts()
    {
        var tierName = $"Merge-Tier-{Guid.NewGuid():N}";

        await using var db = _fixture.CreateContext();

        var monthly = MakePlan(tierName, BillingInterval.Monthly, 19m);
        var yearly = MakePlan(tierName, BillingInterval.Yearly, 180m);

        db.SubscriptionPlans.AddRange(monthly, yearly);
        db.Subscriptions.AddRange(
            MakeSubscription(_businessA.Id, monthly.Id),
            MakeSubscription(_businessB.Id, monthly.Id),
            MakeSubscription(_businessC.Id, yearly.Id));

        await db.SaveChangesAsync();

        var service = CreatePlanService(db);
        var groups = await service.GetGroupsAsync();

        var group = groups.Single(g => g.Name == tierName);
        group.Monthly.Should().NotBeNull();
        group.Yearly.Should().NotBeNull();
        group.Monthly!.Price.Should().Be(19m);
        group.Yearly!.Price.Should().Be(180m);
        group.Monthly.ActiveSubscriberCount.Should().Be(2);
        group.Yearly.ActiveSubscriberCount.Should().Be(1);
        group.TotalActiveSubscriberCount.Should().Be(3, "2 monthly + 1 yearly subscriber on this tier");
    }

    [Fact]
    public async Task Plan_group_reports_only_the_interval_that_exists()
    {
        var tierName = $"MonthlyOnly-Tier-{Guid.NewGuid():N}";

        await using var db = _fixture.CreateContext();

        var monthly = MakePlan(tierName, BillingInterval.Monthly, 9m);
        db.SubscriptionPlans.Add(monthly);
        await db.SaveChangesAsync();

        var service = CreatePlanService(db);
        var groups = await service.GetGroupsAsync();

        var group = groups.Single(g => g.Name == tierName);
        group.Monthly.Should().NotBeNull();
        group.Yearly.Should().BeNull();
    }

    [Fact]
    public async Task Platform_subscription_stats_count_distinct_tiers_and_group_active_subscriptions_by_interval()
    {
        var tierName = $"Stats-Tier-{Guid.NewGuid():N}";

        await using var db = _fixture.CreateContext();

        var statsBefore = await CreatePlanService(db).GetStatsAsync();

        var monthly = MakePlan(tierName, BillingInterval.Monthly, 15m);
        var yearly = MakePlan(tierName, BillingInterval.Yearly, 150m, isActive: false);

        db.SubscriptionPlans.AddRange(monthly, yearly);
        db.Subscriptions.AddRange(
            MakeSubscription(_businessA.Id, monthly.Id),
            MakeSubscription(_businessB.Id, monthly.Id),
            MakeSubscription(_businessC.Id, yearly.Id));

        await db.SaveChangesAsync();

        var statsAfter = await CreatePlanService(db).GetStatsAsync();

        (statsAfter.TotalPlans - statsBefore.TotalPlans).Should().Be(1, "one new distinct tier Name added");
        (statsAfter.ActivePlans - statsBefore.ActivePlans).Should().Be(1, "the tier counts active because its Monthly interval is active, even though Yearly isn't");
        (statsAfter.MonthlySubscriptions - statsBefore.MonthlySubscriptions).Should().Be(2);
        (statsAfter.YearlySubscriptions - statsBefore.YearlySubscriptions).Should().Be(1);
        (statsAfter.SubscribedBusinesses - statsBefore.SubscribedBusinesses).Should().Be(3);
    }

    [Fact]
    public async Task Subscriptions_list_filters_by_plan_billing_interval_and_status()
    {
        var tierName = $"Filter-Tier-{Guid.NewGuid():N}";

        await using var db = _fixture.CreateContext();

        var monthly = MakePlan(tierName, BillingInterval.Monthly, 25m);
        var yearly = MakePlan(tierName, BillingInterval.Yearly, 250m);

        db.SubscriptionPlans.AddRange(monthly, yearly);

        var activeMonthly = MakeSubscription(_businessA.Id, monthly.Id, SubscriptionStatus.Active);
        var cancelledYearly = MakeSubscription(_businessB.Id, yearly.Id, SubscriptionStatus.Cancelled);

        db.Subscriptions.AddRange(activeMonthly, cancelledYearly);
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db);

        var (byPlan, _) = await repository.GetSubscriptionsAsync(
            new SubscriptionsQueryRequest { PlanId = monthly.Id, PageSize = 50 });
        byPlan.Should().ContainSingle(s => s.SubscriptionId == activeMonthly.Id);

        var (byInterval, _) = await repository.GetSubscriptionsAsync(
            new SubscriptionsQueryRequest { BillingInterval = BillingInterval.Yearly, PageSize = 50 });
        byInterval.Should().Contain(s => s.SubscriptionId == cancelledYearly.Id);
        byInterval.Should().NotContain(s => s.SubscriptionId == activeMonthly.Id);

        var (byStatus, _) = await repository.GetSubscriptionsAsync(
            new SubscriptionsQueryRequest { Status = SubscriptionStatus.Active, PageSize = 50 });
        byStatus.Should().Contain(s => s.SubscriptionId == activeMonthly.Id);
        byStatus.Should().NotContain(s => s.SubscriptionId == cancelledYearly.Id);

        var (bySearch, _) = await repository.GetSubscriptionsAsync(
            new SubscriptionsQueryRequest { Search = _businessA.Name, PageSize = 50 });
        bySearch.Should().ContainSingle(s => s.BusinessId == _businessA.Id);
    }

    [Fact]
    public async Task Recent_activity_marks_a_first_subscription_as_new_and_a_second_row_for_the_same_business_as_a_switch()
    {
        var tierName = $"Activity-Tier-{Guid.NewGuid():N}";

        await using var db = _fixture.CreateContext();

        var planOne = MakePlan(tierName + "-One", BillingInterval.Monthly, 10m);
        var planTwo = MakePlan(tierName + "-Two", BillingInterval.Monthly, 20m);
        db.SubscriptionPlans.AddRange(planOne, planTwo);

        var now = DateTime.UtcNow;
        var firstSubscription = MakeSubscription(_businessA.Id, planOne.Id, SubscriptionStatus.Cancelled, createdAt: now.AddDays(-2));
        var switchedSubscription = MakeSubscription(_businessA.Id, planTwo.Id, SubscriptionStatus.Active, createdAt: now.AddDays(-1));
        var otherBusinessFirstSubscription = MakeSubscription(_businessB.Id, planOne.Id, SubscriptionStatus.Active, createdAt: now);

        db.Subscriptions.AddRange(firstSubscription, switchedSubscription, otherBusinessFirstSubscription);
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db);
        var recent = await repository.GetRecentSubscriptionActivityAsync(50);

        var switchEntry = recent.Single(r => r.BusinessId == _businessA.Id && r.PlanName == planTwo.Name);
        switchEntry.IsNewSubscription.Should().BeFalse("this business already had an earlier subscription row");

        var firstEntry = recent.Single(r => r.BusinessId == _businessA.Id && r.PlanName == planOne.Name);
        firstEntry.IsNewSubscription.Should().BeTrue("this was this business's earliest subscription row");

        var otherBusinessEntry = recent.Single(r => r.BusinessId == _businessB.Id);
        otherBusinessEntry.IsNewSubscription.Should().BeTrue();
    }
}
