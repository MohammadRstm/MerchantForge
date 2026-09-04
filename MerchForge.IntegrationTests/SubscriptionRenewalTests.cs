using FluentAssertions;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.Subscription;
using MerchForge.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// Retry-safety for the two subscription-mutating flows: switching plans
/// (BusinessDashboardService.SubscribeToPlanAsync) and the hourly renewal job
/// (RenewSubscriptionPeriodsJob, exercised here at the repository level it's
/// built on - ISubscriptionRepository.TryAdvanceSubscriptionPeriodAsync/
/// TryEndSubscriptionAsync). Against the real database on purpose: what's worth
/// protecting here is that a retried or duplicated request can never duplicate a
/// subscription row or a credit grant, which is a statement about real
/// transaction/row-lock behavior, not something a mock can demonstrate.
/// </summary>
public class SubscriptionRenewalTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;
    private Business _business = null!;
    private Guid _imageEditingFeatureId;

    public SubscriptionRenewalTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _business = await _fixture.CreateBusinessAsync("Renewal Test Co", CatalogDatabaseFixture.FashionDomainId);

        await using var db = _fixture.CreateContext();
        _imageEditingFeatureId = await db.Features
            .Where(f => f.Key == FeatureKeys.AiImageEditing)
            .Select(f => f.Id)
            .FirstAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<SubscriptionPlan> CreatePlanAsync(
        api.Data.MerchForgeDbContext db, string name, int imageEditingLimit)
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = 49m,
            BillingInterval = BillingInterval.Monthly,
        };

        db.SubscriptionPlans.Add(plan);
        db.PlanFeatures.Add(new PlanFeature
        {
            SubscriptionPlanId = plan.Id,
            FeatureId = _imageEditingFeatureId,
            Limit = imageEditingLimit,
        });

        await db.SaveChangesAsync();
        return plan;
    }

    private BusinessDashboardService CreateDashboardService(api.Data.MerchForgeDbContext db)
    {
        var featureCreditRepo = new FeatureCreditRepository(db);
        var subscriptionRepository = new SubscriptionRepository(db);
        var subscriptionService = new SubscriptionService(subscriptionRepository, featureCreditRepo);
        var featureCreditService = new FeatureCreditService(featureCreditRepo, subscriptionService, subscriptionRepository);

        return new BusinessDashboardService(
            new BusinessDashboardRepository(db, TestImageUrls.Resolver),
            subscriptionRepository,
            new WebsiteTemplateRequestRepository(db),
            new OrderRepository(db, TestImageUrls.Resolver),
            new ProductReviewRepository(db),
            new FakeBackgroundJobClient(),
            featureCreditService,
            TestImageUrls.Resolver);
    }

    [Fact]
    public async Task Subscribing_twice_to_the_same_plan_is_a_no_op()
    {
        await using var db = _fixture.CreateContext();
        var plan = await CreatePlanAsync(db, "Growth", imageEditingLimit: 150);
        var dashboardService = CreateDashboardService(db);

        await dashboardService.SubscribeToPlanAsync(_business.Id, plan.Id);
        await dashboardService.SubscribeToPlanAsync(_business.Id, plan.Id);

        await using var verify = _fixture.CreateContext();

        var subscriptions = await verify.Subscriptions
            .Where(s => s.BusinessId == _business.Id)
            .ToListAsync();
        subscriptions.Should().HaveCount(1, "a duplicate subscribe to the same plan must not create a second row");
        subscriptions[0].Status.Should().Be(SubscriptionStatus.Active);

        var balance = await verify.BusinessFeatureCredits
            .FirstAsync(b => b.BusinessId == _business.Id && b.FeatureId == _imageEditingFeatureId);
        balance.CreditsGrantedTotal.Should().Be(150, "the credit grant must not be doubled by the no-op second call");
    }

    [Fact]
    public async Task Switching_plans_cancels_the_old_subscription_and_grants_the_new_plans_credits()
    {
        await using var db = _fixture.CreateContext();
        var starter = await CreatePlanAsync(db, "Starter", imageEditingLimit: 20);
        var growth = await CreatePlanAsync(db, "Growth", imageEditingLimit: 150);
        var dashboardService = CreateDashboardService(db);

        await dashboardService.SubscribeToPlanAsync(_business.Id, starter.Id);
        await dashboardService.SubscribeToPlanAsync(_business.Id, growth.Id);

        await using var verify = _fixture.CreateContext();

        var subscriptions = await verify.Subscriptions
            .Where(s => s.BusinessId == _business.Id)
            .ToListAsync();
        subscriptions.Should().HaveCount(2);
        subscriptions.Should().ContainSingle(s => s.SubscriptionPlanId == starter.Id && s.Status == SubscriptionStatus.Cancelled);
        subscriptions.Should().ContainSingle(s => s.SubscriptionPlanId == growth.Id && s.Status == SubscriptionStatus.Active);

        var balance = await verify.BusinessFeatureCredits
            .FirstAsync(b => b.BusinessId == _business.Id && b.FeatureId == _imageEditingFeatureId);
        balance.CreditsRemaining.Should().Be(150, "the new plan's limit, not the old plan's leftover balance");
    }

    [Fact]
    public async Task Advancing_a_subscription_period_twice_with_the_same_expected_end_only_succeeds_once()
    {
        await using var db = _fixture.CreateContext();
        var plan = await CreatePlanAsync(db, "Pro", imageEditingLimit: 400);

        var periodEnd = DateTime.UtcNow.AddDays(-1);
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            BusinessId = _business.Id,
            SubscriptionPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = periodEnd.AddMonths(-1),
            CurrentPeriodEnd = periodEnd,
        };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var repo = new SubscriptionRepository(db);
        var newStart = periodEnd;
        var newEnd = periodEnd.AddMonths(1);

        // Simulates a Hangfire retry: two callers both read the same
        // CurrentPeriodEnd before either had advanced it.
        var first = await repo.TryAdvanceSubscriptionPeriodAsync(
            subscription.Id, periodEnd, newStart, newEnd, DateTime.UtcNow);
        var second = await repo.TryAdvanceSubscriptionPeriodAsync(
            subscription.Id, periodEnd, newStart, newEnd, DateTime.UtcNow);

        first.Should().BeTrue("the first attempt genuinely advances the period");
        second.Should().BeFalse("the period no longer matches what the second attempt expected, so it must no-op");

        await using var verify = _fixture.CreateContext();
        var updated = await verify.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id);
        // MySQL's DATETIME(6) rounds sub-microsecond precision on round-trip, so an
        // exact comparison against the in-memory tick-precision value is too strict.
        updated.CurrentPeriodEnd.Should().BeCloseTo(newEnd, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Ending_a_subscription_twice_only_succeeds_once()
    {
        await using var db = _fixture.CreateContext();
        var plan = await CreatePlanAsync(db, "Starter", imageEditingLimit: 20);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            BusinessId = _business.Id,
            SubscriptionPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1),
            CancelAtPeriodEnd = true,
        };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var repo = new SubscriptionRepository(db);

        var first = await repo.TryEndSubscriptionAsync(subscription.Id, DateTime.UtcNow);
        var second = await repo.TryEndSubscriptionAsync(subscription.Id, DateTime.UtcNow);

        first.Should().BeTrue();
        second.Should().BeFalse("it's already Cancelled, so a retry finding it must no-op rather than re-notify");
    }
}
