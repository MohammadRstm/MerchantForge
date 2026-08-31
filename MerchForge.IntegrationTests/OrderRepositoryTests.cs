using FluentAssertions;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Orders;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// Order creation and cancellation are the app's most transactionally complex
/// write paths - stock decrement/restock, a StockMovement ledger entry, and the
/// order/order-item/status-history rows all have to land together or not at all.
/// Against the real database on purpose: what's worth protecting here is that a
/// failure partway through a multi-item order rolls back everything already
/// written, which is a statement about real transaction behavior, not something a
/// mock can demonstrate.
/// </summary>
public class OrderRepositoryTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;
    private Business _business = null!;

    public OrderRepositoryTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _business = await _fixture.CreateBusinessAsync("Order Test Co", CatalogDatabaseFixture.FashionDomainId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Product> CreateProductAsync(int? stockQuantity, decimal price = 25m)
    {
        await using var db = _fixture.CreateContext();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = _business.Id,
            CategoryId = CatalogDatabaseFixture.ShirtsCategoryId,
            Title = $"Product {Guid.NewGuid():N}",
            Price = price,
            StockQuantity = stockQuantity,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private static CreateOrderRequest BuildRequest(params (Guid ProductId, int Quantity)[] items) => new()
    {
        CustomerName = "Jane Shopper",
        CustomerEmail = "jane@example.com",
        ShippingAddressLine1 = "123 Main St",
        ShippingCity = "Springfield",
        ShippingPostalCode = "00000",
        ShippingCountry = "US",
        Items = items.Select(i => new CreateOrderItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList(),
    };

    [Fact]
    public async Task Creating_an_order_decrements_tracked_stock_and_records_a_stock_movement()
    {
        var product = await CreateProductAsync(stockQuantity: 10, price: 20m);
        await using var db = _fixture.CreateContext();
        var repo = new OrderRepository(db);

        var order = await repo.CreateOrderAsync(
            _business.Id, BuildRequest((product.Id, 3)), customerId: null);

        order.Subtotal.Should().Be(60m);
        order.Total.Should().Be(60m);
        order.Status.Should().Be(OrderStatus.Pending);

        await using var verify = _fixture.CreateContext();
        var updatedProduct = await verify.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        updatedProduct.StockQuantity.Should().Be(7);

        var movement = await verify.StockMovements.AsNoTracking().SingleAsync(m => m.ProductId == product.Id);
        movement.Amount.Should().Be(-3);
        movement.BalanceAfter.Should().Be(7);

        var history = await verify.OrderStatusHistories.AsNoTracking().SingleAsync(h => h.OrderId == order.Id);
        history.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task Creating_an_order_never_touches_stock_for_an_untracked_product()
    {
        var product = await CreateProductAsync(stockQuantity: null);
        await using var db = _fixture.CreateContext();
        var repo = new OrderRepository(db);

        await repo.CreateOrderAsync(_business.Id, BuildRequest((product.Id, 5)), customerId: null);

        await using var verify = _fixture.CreateContext();
        var updatedProduct = await verify.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        updatedProduct.StockQuantity.Should().BeNull("untracked stays untracked regardless of sales");

        var movementExists = await verify.StockMovements.AnyAsync(m => m.ProductId == product.Id);
        movementExists.Should().BeFalse();
    }

    [Fact]
    public async Task Creating_an_order_fails_for_a_product_that_does_not_belong_to_this_business()
    {
        var otherBusiness = await _fixture.CreateBusinessAsync("Other Co", CatalogDatabaseFixture.FashionDomainId);
        await using var seed = _fixture.CreateContext();
        var foreignProduct = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = otherBusiness.Id,
            CategoryId = CatalogDatabaseFixture.ShirtsCategoryId,
            Title = "Foreign Product",
            Price = 10m,
            StockQuantity = 5,
        };
        seed.Products.Add(foreignProduct);
        await seed.SaveChangesAsync();

        await using var db = _fixture.CreateContext();
        var repo = new OrderRepository(db);

        var act = () => repo.CreateOrderAsync(_business.Id, BuildRequest((foreignProduct.Id, 1)), customerId: null);

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Creating_an_order_fails_when_requested_quantity_exceeds_stock()
    {
        var product = await CreateProductAsync(stockQuantity: 2);
        await using var db = _fixture.CreateContext();
        var repo = new OrderRepository(db);

        var act = () => repo.CreateOrderAsync(_business.Id, BuildRequest((product.Id, 3)), customerId: null);

        await act.Should().ThrowAsync<InsufficientStockForOrderException>();
    }

    [Fact]
    public async Task A_failure_partway_through_a_multi_item_order_rolls_back_every_stock_change()
    {
        // The first item has enough stock and would succeed on its own; the second
        // doesn't. The whole order - including the first item's decrement - must be
        // rolled back, not left half-applied.
        var affordable = await CreateProductAsync(stockQuantity: 10);
        var insufficient = await CreateProductAsync(stockQuantity: 1);

        await using var db = _fixture.CreateContext();
        var repo = new OrderRepository(db);

        var act = () => repo.CreateOrderAsync(
            _business.Id, BuildRequest((affordable.Id, 2), (insufficient.Id, 5)), customerId: null);

        await act.Should().ThrowAsync<InsufficientStockForOrderException>();

        await using var verify = _fixture.CreateContext();
        var affordableAfter = await verify.Products.AsNoTracking().FirstAsync(p => p.Id == affordable.Id);
        affordableAfter.StockQuantity.Should().Be(10, "the first item's decrement must be rolled back with the rest of the failed order");

        var anyOrderCreated = await verify.Orders.AnyAsync(o => o.BusinessId == _business.Id);
        anyOrderCreated.Should().BeFalse();

        var anyMovementCreated = await verify.StockMovements.AnyAsync(m => m.ProductId == affordable.Id || m.ProductId == insufficient.Id);
        anyMovementCreated.Should().BeFalse();
    }

    [Fact]
    public async Task Cancelling_an_order_restocks_tracked_products_and_records_a_positive_stock_movement()
    {
        var product = await CreateProductAsync(stockQuantity: 10);
        await using var db = _fixture.CreateContext();
        var repo = new OrderRepository(db);

        var order = await repo.CreateOrderAsync(_business.Id, BuildRequest((product.Id, 4)), customerId: null);

        var tracked = await repo.GetTrackedOrderAsync(_business.Id, order.Id) ?? throw new InvalidOperationException();
        await repo.UpdateOrderStatusAsync(tracked, OrderStatus.Cancelled, changedByUserId: Guid.NewGuid());

        await using var verify = _fixture.CreateContext();
        var updatedProduct = await verify.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        updatedProduct.StockQuantity.Should().Be(10, "cancelling must give back exactly what the order took");

        var movements = await verify.StockMovements.AsNoTracking()
            .Where(m => m.ProductId == product.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
        movements.Should().HaveCount(2);
        movements[0].Amount.Should().Be(-4);
        movements[1].Amount.Should().Be(4, "the restock is a positive ledger entry, mirroring the original decrement");

        var updatedOrder = await verify.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        updatedOrder.Status.Should().Be(OrderStatus.Cancelled);

        var history = await verify.OrderStatusHistories.AsNoTracking()
            .Where(h => h.OrderId == order.Id)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();
        history.Should().HaveCount(2);
        history[1].Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancelling_an_order_does_not_touch_stock_for_a_product_that_was_untracked_at_order_time()
    {
        var product = await CreateProductAsync(stockQuantity: null);
        await using var db = _fixture.CreateContext();
        var repo = new OrderRepository(db);

        var order = await repo.CreateOrderAsync(_business.Id, BuildRequest((product.Id, 2)), customerId: null);
        var tracked = await repo.GetTrackedOrderAsync(_business.Id, order.Id) ?? throw new InvalidOperationException();

        await repo.UpdateOrderStatusAsync(tracked, OrderStatus.Cancelled, changedByUserId: Guid.NewGuid());

        await using var verify = _fixture.CreateContext();
        var updatedProduct = await verify.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        updatedProduct.StockQuantity.Should().BeNull();

        var movementExists = await verify.StockMovements.AnyAsync(m => m.ProductId == product.Id);
        movementExists.Should().BeFalse("no decrement happened at order time, so cancelling has nothing to reverse");
    }
}
