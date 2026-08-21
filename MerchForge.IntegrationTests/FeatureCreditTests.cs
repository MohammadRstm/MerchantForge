using FluentAssertions;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.Subscription;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// Features bought independently of a subscription plan: buying credits, spending
/// them, and the two routes into a feature - plan membership and credit balance -
/// staying correctly independent of each other.
///
/// Against the real database on purpose, the same reasoning as the rest of this
/// project's integration tests: the property actually worth protecting here is that
/// concurrent spends can never drive a balance negative, which is a statement about
/// real row locking, not something a mock can demonstrate.
/// </summary>
public class FeatureCreditTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _business = null!;
    private Guid _featureId;
    private Guid _starterPackageId;
    private const int StarterCredits = 50;

    public FeatureCreditTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _business = await _fixture.CreateBusinessAsync("Credit Test Co", CatalogDatabaseFixture.FashionDomainId);

        await using var db = _fixture.CreateContext();

        var feature = await db.Features.FirstAsync(f => f.Key == FeatureKeys.AiProductGeneration);
        _featureId = feature.Id;

        var starter = await db.FeatureCreditPackages
            .FirstAsync(p => p.FeatureId == _featureId && p.Name == "Starter");
        _starterPackageId = starter.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static (FeatureCreditRepository Repo, SubscriptionService Subscription, FeatureCreditService Credits)
        CreateServices(api.Data.MerchForgeDbContext db)
    {
        var repo = new FeatureCreditRepository(db);
        var subscription = new SubscriptionService(new SubscriptionRepository(db), repo);
        var credits = new FeatureCreditService(repo, subscription);

        return (repo, subscription, credits);
    }

    [Fact]
    public async Task A_business_with_no_purchase_and_no_plan_has_no_access()
    {
        await using var db = _fixture.CreateContext();
        var (_, subscription, _) = CreateServices(db);

        (await subscription.HasFeatureAsync(_business.Id, FeatureKeys.AiProductGeneration))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Buying_a_package_grants_access_and_credits()
    {
        await using var db = _fixture.CreateContext();
        var (_, subscription, credits) = CreateServices(db);

        var result = await credits.PurchaseAsync(_business.Id, _starterPackageId);

        result.CreditsRemaining.Should().Be(StarterCredits);
        result.CreditsGrantedTotal.Should().Be(StarterCredits);
        (await subscription.HasFeatureAsync(_business.Id, FeatureKeys.AiProductGeneration))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Buying_a_second_package_tops_up_rather_than_overwrites()
    {
        await using var db = _fixture.CreateContext();
        var (_, _, credits) = CreateServices(db);

        await credits.PurchaseAsync(_business.Id, _starterPackageId);
        var second = await credits.PurchaseAsync(_business.Id, _starterPackageId);

        second.CreditsRemaining.Should().Be(StarterCredits * 2);
        second.CreditsGrantedTotal.Should().Be(StarterCredits * 2);
    }

    [Fact]
    public async Task Consuming_a_credit_decrements_the_balance_and_records_a_ledger_entry()
    {
        await using var db = _fixture.CreateContext();
        var (_, _, credits) = CreateServices(db);

        await credits.PurchaseAsync(_business.Id, _starterPackageId);

        var consumed = await credits.TryConsumeAsync(_business.Id, FeatureKeys.AiProductGeneration, "draft-1");

        consumed.Should().BeTrue();

        var balance = await db.BusinessFeatureCredits.AsNoTracking()
            .FirstAsync(b => b.BusinessId == _business.Id && b.FeatureId == _featureId);
        balance.CreditsRemaining.Should().Be(StarterCredits - 1);

        var ledger = await db.FeatureCreditTransactions.AsNoTracking()
            .Where(t => t.BusinessFeatureCreditId == balance.Id)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        ledger.Should().HaveCount(2, "one entry for the purchase, one for the spend");
        ledger[1].Type.Should().Be(FeatureCreditTransactionType.Consumption);
        ledger[1].Amount.Should().Be(-1);
        ledger[1].BalanceAfter.Should().Be(StarterCredits - 1);
        ledger[1].Reference.Should().Be("draft-1");
    }

    [Fact]
    public async Task Consuming_with_no_balance_fails_without_throwing()
    {
        await using var db = _fixture.CreateContext();
        var (_, _, credits) = CreateServices(db);

        var consumed = await credits.TryConsumeAsync(_business.Id, FeatureKeys.AiProductGeneration, null);

        consumed.Should().BeFalse();
    }

    [Fact]
    public async Task A_plan_bundled_feature_is_never_metered()
    {
        await using var db = _fixture.CreateContext();

        // Bundle the feature into a plan and subscribe this business to it, without
        // ever buying a single credit.
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Pro",
            Price = 99m,
            BillingInterval = BillingInterval.Monthly,
        };
        db.SubscriptionPlans.Add(plan);
        db.PlanFeatures.Add(new PlanFeature { SubscriptionPlanId = plan.Id, FeatureId = _featureId });
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            BusinessId = _business.Id,
            SubscriptionPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        });
        await db.SaveChangesAsync();

        var (_, subscription, credits) = CreateServices(db);

        (await subscription.HasFeatureAsync(_business.Id, FeatureKeys.AiProductGeneration))
            .Should().BeTrue();

        var consumed = await credits.TryConsumeAsync(_business.Id, FeatureKeys.AiProductGeneration, null);
        consumed.Should().BeTrue("plan membership makes usage unlimited, not zero-credit");

        var balanceExists = await db.BusinessFeatureCredits.AnyAsync(b => b.BusinessId == _business.Id);
        balanceExists.Should().BeFalse("plan-bundled usage is never metered into a balance row");
    }

    [Fact]
    public async Task Concurrent_consumption_never_drives_the_balance_negative()
    {
        await using (var setup = _fixture.CreateContext())
        {
            var (_, _, credits) = CreateServices(setup);
            await credits.PurchaseAsync(_business.Id, _starterPackageId);
        }

        const int attempts = StarterCredits + 15;

        // Each attempt gets its own DbContext/connection - a DbContext isn't
        // thread-safe, and the point of this test is real concurrent connections
        // racing the database's row lock, not concurrent calls sharing one context.
        var results = await Task.WhenAll(Enumerable.Range(0, attempts).Select(async i =>
        {
            await using var db = _fixture.CreateContext();
            var repo = new FeatureCreditRepository(db);

            return await repo.TryConsumeCreditAsync(
                _business.Id, FeatureKeys.AiProductGeneration, $"attempt-{i}");
        }));

        results.Count(succeeded => succeeded).Should().Be(StarterCredits,
            "exactly as many attempts as there were credits should succeed, no more");

        await using var verify = _fixture.CreateContext();
        var balance = await verify.BusinessFeatureCredits.AsNoTracking()
            .FirstAsync(b => b.BusinessId == _business.Id && b.FeatureId == _featureId);

        balance.CreditsRemaining.Should().Be(0, "never negative, and never left with unspent credit either");
    }
}
