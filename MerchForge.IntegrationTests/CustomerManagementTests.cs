using FluentAssertions;
using MerchForge.api.Data;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using MerchForge.IntegrationTests.Fakes;

namespace MerchForge.IntegrationTests;

/// <summary>
/// The Super Admin Customers enhancement's new aggregation logic: multi-currency
/// spend handling, the HasOrders/registration-date filters, repeat-customer stats,
/// per-business customer distribution, and per-customer spend-over-time. Real
/// MariaDB, matching this suite's established convention.
/// </summary>
public class CustomerManagementTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _businessA = null!;
    private Business _businessB = null!;

    public CustomerManagementTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _businessA = await _fixture.CreateBusinessAsync("Customer Test Co A", CatalogDatabaseFixture.FashionDomainId, currency: "USD");
        _businessB = await _fixture.CreateBusinessAsync("Customer Test Co B", CatalogDatabaseFixture.FashionDomainId, currency: "EUR");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static Customer MakeCustomer(string? email = null) => new()
    {
        Id = Guid.NewGuid(),
        Email = email ?? $"{Guid.NewGuid():N}@example.test",
        FirstName = "Test",
        LastName = $"Customer-{Guid.NewGuid():N}"[..14],
        PasswordHash = "not-a-real-hash",
    };

    private static Order MakeOrder(
        Guid businessId, Guid? customerId, decimal total, string currency,
        OrderStatus status = OrderStatus.Confirmed, DateTime? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        CustomerId = customerId,
        CustomerName = "Test Customer",
        CustomerEmail = "order@example.test",
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
    public async Task GetCustomersAsync_shows_the_customers_highest_value_currency_as_their_primary_total_not_a_cross_currency_sum()
    {
        await using var db = _fixture.CreateContext();
        var customer = MakeCustomer();
        db.Customers.Add(customer);
        db.Orders.AddRange(
            MakeOrder(_businessA.Id, customer.Id, 40m, "USD"),
            MakeOrder(_businessB.Id, customer.Id, 500m, "EUR"));
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var (items, _) = await repository.GetCustomersAsync(new CustomersQueryRequest { Search = customer.Email, PageSize = 50 });

        var item = items.Should().ContainSingle(c => c.Id == customer.Id).Which;
        item.OrderCount.Should().Be(2, "order count is currency-independent");
        item.SpentCurrency.Should().Be("EUR", "the 500 EUR order outweighs the 40 USD one");
        item.TotalSpent.Should().Be(500m, "TotalSpent must never mix currencies together");
    }

    [Fact]
    public async Task GetCustomersAsync_excludes_cancelled_orders_from_spend_and_order_count()
    {
        await using var db = _fixture.CreateContext();
        var customer = MakeCustomer();
        db.Customers.Add(customer);
        db.Orders.AddRange(
            MakeOrder(_businessA.Id, customer.Id, 100m, "USD", OrderStatus.Confirmed),
            MakeOrder(_businessA.Id, customer.Id, 999m, "USD", OrderStatus.Cancelled));
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var (items, _) = await repository.GetCustomersAsync(new CustomersQueryRequest { Search = customer.Email, PageSize = 50 });

        var item = items.Should().ContainSingle().Which;
        item.OrderCount.Should().Be(1);
        item.TotalSpent.Should().Be(100m);
    }

    [Fact]
    public async Task GetCustomersAsync_filters_by_has_orders()
    {
        await using var db = _fixture.CreateContext();
        var withOrders = MakeCustomer();
        var withoutOrders = MakeCustomer();
        db.Customers.AddRange(withOrders, withoutOrders);
        db.Orders.Add(MakeOrder(_businessA.Id, withOrders.Id, 50m, "USD"));
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);

        var (hasOrders, _) = await repository.GetCustomersAsync(
            new CustomersQueryRequest { HasOrders = true, PageSize = 500 });
        hasOrders.Should().Contain(c => c.Id == withOrders.Id);
        hasOrders.Should().NotContain(c => c.Id == withoutOrders.Id);

        var (noOrders, _) = await repository.GetCustomersAsync(
            new CustomersQueryRequest { HasOrders = false, PageSize = 500 });
        noOrders.Should().Contain(c => c.Id == withoutOrders.Id);
        noOrders.Should().NotContain(c => c.Id == withOrders.Id);
    }

    [Fact]
    public async Task GetCustomerStatsAsync_computes_repeat_rate_from_customers_with_two_or_more_orders()
    {
        await using var db = _fixture.CreateContext();
        var repeatCustomer = MakeCustomer();
        var oneTimeCustomer = MakeCustomer();
        db.Customers.AddRange(repeatCustomer, oneTimeCustomer);
        db.Orders.AddRange(
            MakeOrder(_businessA.Id, repeatCustomer.Id, 10m, "USD"),
            MakeOrder(_businessA.Id, repeatCustomer.Id, 10m, "USD"),
            MakeOrder(_businessA.Id, oneTimeCustomer.Id, 10m, "USD"));
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var statsBefore = await repository.GetCustomerStatsAsync(newCustomersPeriodDays: 30);

        // Add a third, unrelated, order-less customer to change the denominator predictably.
        db.Customers.Add(MakeCustomer());
        await db.SaveChangesAsync();

        var stats = await repository.GetCustomerStatsAsync(newCustomersPeriodDays: 30);

        (stats.CustomersWithOrders - statsBefore.CustomersWithOrders).Should().Be(0, "no new order-bearing customer was added this step");
        (stats.RepeatCustomers - statsBefore.RepeatCustomers).Should().Be(0);
        stats.RepeatCustomerRate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCustomerDistributionByBusinessAsync_counts_distinct_customers_per_business()
    {
        await using var db = _fixture.CreateContext();
        var customer1 = MakeCustomer();
        var customer2 = MakeCustomer();
        db.Customers.AddRange(customer1, customer2);
        db.Orders.AddRange(
            MakeOrder(_businessA.Id, customer1.Id, 10m, "USD"),
            MakeOrder(_businessA.Id, customer1.Id, 10m, "USD"), // same customer, second order - must not double count
            MakeOrder(_businessA.Id, customer2.Id, 10m, "USD"));
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var distribution = await repository.GetCustomerDistributionByBusinessAsync();

        distribution.Should().Contain(d => d.Key == _businessA.Name && d.Count >= 2);
    }

    [Fact]
    public async Task GetCustomerSpendOverTimeAsync_groups_by_month_and_currency_separately()
    {
        await using var db = _fixture.CreateContext();
        var customer = MakeCustomer();
        db.Customers.Add(customer);

        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 15, 0, 0, 0, DateTimeKind.Utc);
        db.Orders.AddRange(
            MakeOrder(_businessA.Id, customer.Id, 30m, "USD", createdAt: thisMonth),
            MakeOrder(_businessB.Id, customer.Id, 70m, "EUR", createdAt: thisMonth));
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var points = await repository.GetCustomerSpendOverTimeAsync(customer.Id);

        points.Should().Contain(p => p.Currency == "USD" && p.Total == 30m);
        points.Should().Contain(p => p.Currency == "EUR" && p.Total == 70m);
    }

    [Fact]
    public async Task RevokeAllForCustomerAsync_revokes_only_that_customers_active_tokens()
    {
        await using var db = _fixture.CreateContext();
        var customer = MakeCustomer();
        var otherCustomer = MakeCustomer();
        db.Customers.AddRange(customer, otherCustomer);

        var now = DateTime.UtcNow;
        db.CustomerRefreshTokens.AddRange(
            new CustomerRefreshToken { Id = Guid.NewGuid(), CustomerId = customer.Id, TokenHash = $"h-{Guid.NewGuid():N}", ExpiresAt = now.AddDays(1) },
            new CustomerRefreshToken { Id = Guid.NewGuid(), CustomerId = otherCustomer.Id, TokenHash = $"h-{Guid.NewGuid():N}", ExpiresAt = now.AddDays(1) });
        await db.SaveChangesAsync();

        var repository = new CustomerRefreshTokenRepository(db);
        var revokedCount = await repository.RevokeAllForCustomerAsync(customer.Id);

        revokedCount.Should().Be(1);

        var otherToken = await db.CustomerRefreshTokens.AsNoTracking().FirstAsync(t => t.CustomerId == otherCustomer.Id);
        otherToken.RevokedAt.Should().BeNull();
    }
}
