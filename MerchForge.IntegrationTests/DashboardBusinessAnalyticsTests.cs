using FluentAssertions;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using MerchForge.IntegrationTests.Fakes;

namespace MerchForge.IntegrationTests;

/// <summary>
/// The Super Admin Businesses list/detail enhancement's new aggregation logic:
/// per-business order/revenue rollups on the paged business list, platform-wide
/// currency-grouped revenue, and the new customers-by-business filter. Exercised
/// against DashboardRepository directly, matching this suite's existing
/// repository-level convention (see ProductCrudTests's own doc comment for why real
/// MariaDB, not a double).
/// </summary>
public class DashboardBusinessAnalyticsTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _businessWithOrders = null!;
    private Business _businessWithoutOrders = null!;

    public DashboardBusinessAnalyticsTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _businessWithOrders = await _fixture.CreateBusinessAsync("Analytics Fashion Co", CatalogDatabaseFixture.FashionDomainId);
        _businessWithoutOrders = await _fixture.CreateBusinessAsync("Analytics Quiet Co", CatalogDatabaseFixture.FashionDomainId, currency: "EUR");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static Order MakeOrder(Guid businessId, decimal total, OrderStatus status, string currency = "USD", DateTime? createdAt = null, Guid? customerId = null) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        CustomerId = customerId,
        CustomerName = "Test Customer",
        CustomerEmail = $"{Guid.NewGuid():N}@example.test",
        ShippingAddressLine1 = "1 Test St",
        ShippingCity = "Testville",
        ShippingPostalCode = "00000",
        ShippingCountry = "US",
        Status = status,
        Subtotal = total,
        Total = total,
        Currency = currency,
        CreatedAt = createdAt ?? DateTime.UtcNow,
        UpdatedAt = createdAt ?? DateTime.UtcNow,
    };

    [Fact]
    public async Task Business_list_reports_order_count_and_recorded_revenue_excluding_cancelled_orders()
    {
        await using var db = _fixture.CreateContext();

        var oldOrderDate = DateTime.UtcNow.AddDays(-5);
        var recentOrderDate = DateTime.UtcNow.AddDays(-1);

        db.Orders.AddRange(
            MakeOrder(_businessWithOrders.Id, 50m, OrderStatus.Confirmed, createdAt: oldOrderDate),
            MakeOrder(_businessWithOrders.Id, 30m, OrderStatus.Delivered, createdAt: recentOrderDate),
            MakeOrder(_businessWithOrders.Id, 999m, OrderStatus.Cancelled, createdAt: recentOrderDate));

        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);

        var (items, _) = await repository.GetBusinessesAsync(new BusinessesQueryRequest { PageSize = 50 });

        var withOrders = items.Single(b => b.Id == _businessWithOrders.Id);
        withOrders.OrderCount.Should().Be(2, "the cancelled order must not count");
        withOrders.RecordedRevenue.Should().Be(80m, "50 + 30, excluding the cancelled 999 order");
        withOrders.RevenueCurrency.Should().Be("USD");
        withOrders.LastOrderAt.Should().BeCloseTo(recentOrderDate, TimeSpan.FromSeconds(1));

        var withoutOrders = items.Single(b => b.Id == _businessWithoutOrders.Id);
        withoutOrders.OrderCount.Should().Be(0);
        withoutOrders.RecordedRevenue.Should().Be(0m);
        withoutOrders.LastOrderAt.Should().BeNull();
    }

    [Fact]
    public async Task Business_list_reports_no_plan_info_when_no_subscription_exists()
    {
        await using var db = _fixture.CreateContext();
        var repository = new DashboardRepository(db, TestImageUrls.Resolver);

        var (items, _) = await repository.GetBusinessesAsync(new BusinessesQueryRequest { PageSize = 50 });

        var business = items.Single(b => b.Id == _businessWithoutOrders.Id);
        business.PlanName.Should().BeNull();
        business.BillingInterval.Should().BeNull();
        business.SubscriptionStatus.Should().BeNull();
    }

    [Fact]
    public async Task Platform_revenue_is_grouped_by_currency_and_excludes_cancelled_orders()
    {
        await using var db = _fixture.CreateContext();
        var repository = new DashboardRepository(db, TestImageUrls.Resolver);

        // The fixture's database is shared across every test method in this class, so
        // other tests' orders are already in it - assert the delta this test's own
        // orders contribute, not an absolute total.
        var before = await repository.GetRecordedOrderRevenueByCurrencyAsync();
        var usdBefore = before.SingleOrDefault(t => t.Currency == "USD");
        var eurBefore = before.SingleOrDefault(t => t.Currency == "EUR");

        db.Orders.AddRange(
            MakeOrder(_businessWithOrders.Id, 100m, OrderStatus.Confirmed, currency: "USD"),
            MakeOrder(_businessWithOrders.Id, 50m, OrderStatus.Delivered, currency: "USD"),
            MakeOrder(_businessWithoutOrders.Id, 40m, OrderStatus.Confirmed, currency: "EUR"),
            MakeOrder(_businessWithOrders.Id, 10_000m, OrderStatus.Cancelled, currency: "USD"));

        await db.SaveChangesAsync();

        var after = await repository.GetRecordedOrderRevenueByCurrencyAsync();
        var usdAfter = after.Single(t => t.Currency == "USD");
        var eurAfter = after.Single(t => t.Currency == "EUR");

        (usdAfter.Total - (usdBefore?.Total ?? 0m)).Should().Be(150m, "100 + 50, excluding the cancelled 10,000 order");
        (usdAfter.OrderCount - (usdBefore?.OrderCount ?? 0)).Should().Be(2);

        (eurAfter.Total - (eurBefore?.Total ?? 0m)).Should().Be(40m);
        (eurAfter.OrderCount - (eurBefore?.OrderCount ?? 0)).Should().Be(1);
    }

    [Fact]
    public async Task CountOrdersAsync_excludes_cancelled_orders_platform_wide()
    {
        await using var db = _fixture.CreateContext();
        var repository = new DashboardRepository(db, TestImageUrls.Resolver);

        var before = await repository.CountOrdersAsync();

        db.Orders.AddRange(
            MakeOrder(_businessWithOrders.Id, 10m, OrderStatus.Pending),
            MakeOrder(_businessWithOrders.Id, 10m, OrderStatus.Cancelled));

        await db.SaveChangesAsync();

        var after = await repository.CountOrdersAsync();

        (after - before).Should().Be(1, "only the Pending order should count, not the Cancelled one");
    }

    [Fact]
    public async Task Customers_query_filtered_by_business_only_returns_customers_who_ordered_from_it()
    {
        await using var db = _fixture.CreateContext();

        var systemRoleId = await db.SystemRoles.Where(r => r.Role == SystemRole.User).Select(r => r.Id).FirstAsync();
        var customerForThisBusiness = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Buyer",
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = "not-a-real-hash",
        };
        var customerForOtherBusiness = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Bea",
            LastName = "Elsewhere",
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = "not-a-real-hash",
        };

        db.Customers.AddRange(customerForThisBusiness, customerForOtherBusiness);
        db.Orders.AddRange(
            MakeOrder(_businessWithOrders.Id, 20m, OrderStatus.Confirmed, customerId: customerForThisBusiness.Id),
            MakeOrder(_businessWithoutOrders.Id, 20m, OrderStatus.Confirmed, customerId: customerForOtherBusiness.Id));

        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);

        var (items, totalCount) = await repository.GetCustomersAsync(
            new CustomersQueryRequest { BusinessId = _businessWithOrders.Id, PageSize = 50 });

        items.Should().ContainSingle(c => c.Id == customerForThisBusiness.Id);
        items.Should().NotContain(c => c.Id == customerForOtherBusiness.Id);
        totalCount.Should().Be(1);
    }
}
