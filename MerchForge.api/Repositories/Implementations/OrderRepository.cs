using MerchForge.api.Data;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Orders;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations;

public class OrderRepository : IOrderRepository
{
    private readonly MerchForgeDbContext _db;

    public OrderRepository(MerchForgeDbContext db)
    {
        _db = db;
    }

    // ---- storefront ----

    public async Task<Order> CreateOrderAsync(
        Guid businessId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

            var products = await _db.Products
                .Where(p => p.BusinessId == businessId && productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var now = DateTime.UtcNow;
            var order = new Order
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CustomerName = request.CustomerName.Trim(),
                CustomerEmail = request.CustomerEmail.Trim(),
                CustomerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? null : request.CustomerPhone.Trim(),
                ShippingAddressLine1 = request.ShippingAddressLine1.Trim(),
                ShippingAddressLine2 = string.IsNullOrWhiteSpace(request.ShippingAddressLine2) ? null : request.ShippingAddressLine2.Trim(),
                ShippingCity = request.ShippingCity.Trim(),
                ShippingState = string.IsNullOrWhiteSpace(request.ShippingState) ? null : request.ShippingState.Trim(),
                ShippingPostalCode = request.ShippingPostalCode.Trim(),
                ShippingCountry = request.ShippingCountry.Trim(),
                CustomerNotes = string.IsNullOrWhiteSpace(request.CustomerNotes) ? null : request.CustomerNotes.Trim(),
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var shortRef = order.Id.ToString("N")[..8];
            decimal subtotal = 0;

            foreach (var requested in request.Items)
            {
                // A product id the storefront submitted that doesn't belong to (or
                // doesn't exist in) this business — reuses ProductNotFoundException
                // since the failure mode is identical from the caller's point of view.
                if (!products.TryGetValue(requested.ProductId, out var product))
                {
                    throw new ProductNotFoundException();
                }

                // Untracked (null StockQuantity) products are never decremented or
                // stock-checked — "not tracked" means the merchant doesn't want
                // inventory managed for this item at all, a sale doesn't change that.
                if (product.StockQuantity is int currentStock)
                {
                    var newStock = currentStock - requested.Quantity;

                    if (newStock < 0)
                    {
                        throw new InsufficientStockForOrderException(product.Title);
                    }

                    product.StockQuantity = newStock;
                    product.UpdatedAt = now;

                    await _db.StockMovements.AddAsync(
                        new StockMovement
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            BusinessId = businessId,
                            Amount = -requested.Quantity,
                            BalanceAfter = newStock,
                            Reason = $"Order #{shortRef}",
                            // No dashboard user initiated this — Guid.Empty marks a
                            // system/customer-driven movement, same sentinel
                            // UpdateOrderStatusAsync's restock uses on cancellation.
                            CreatedByUserId = Guid.Empty,
                            CreatedAt = now,
                        },
                        cancellationToken);
                }

                var lineTotal = product.Price * requested.Quantity;
                subtotal += lineTotal;

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    ProductTitle = product.Title,
                    ProductImageUrl = product.ImageUrl,
                    UnitPrice = product.Price,
                    Quantity = requested.Quantity,
                    LineTotal = lineTotal,
                });
            }

            order.Subtotal = subtotal;
            // Equal to Subtotal today — see Order.Total's own doc comment.
            order.Total = subtotal;

            var business = await _db.Businesses
                .AsNoTracking()
                .Where(b => b.Id == businessId)
                .Select(b => new { b.Currency })
                .FirstOrDefaultAsync(cancellationToken);

            order.Currency = business?.Currency ?? "USD";

            await _db.Orders.AddAsync(order, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return order;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StorefrontOrderResponse?> GetOrderForStorefrontAsync(
        Guid businessId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.BusinessId == businessId)
            .Select(o => new StorefrontOrderResponse
            {
                Id = o.Id,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                CustomerPhone = o.CustomerPhone,
                ShippingAddressLine1 = o.ShippingAddressLine1,
                ShippingAddressLine2 = o.ShippingAddressLine2,
                ShippingCity = o.ShippingCity,
                ShippingState = o.ShippingState,
                ShippingPostalCode = o.ShippingPostalCode,
                ShippingCountry = o.ShippingCountry,
                CustomerNotes = o.CustomerNotes,
                Subtotal = o.Subtotal,
                Total = o.Total,
                Currency = o.Currency,
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new StorefrontOrderItemResponse
                {
                    ProductId = i.ProductId,
                    ProductTitle = i.ProductTitle,
                    ProductImageUrl = i.ProductImageUrl,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal,
                }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    // ---- dashboard ----

    public async Task<(List<BusinessOrderResponse> Items, int TotalCount)> GetOrdersAsync(
        Guid businessId,
        OrdersQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _db.Orders.Where(o => o.BusinessId == businessId);

        if (query.Status.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";

            baseQuery = baseQuery.Where(o =>
                EF.Functions.Like(o.CustomerName, pattern) || EF.Functions.Like(o.CustomerEmail, pattern));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => new BusinessOrderResponse
            {
                Id = o.Id,
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                Total = o.Total,
                Currency = o.Currency,
                ItemCount = o.Items.Count,
                CreatedAt = o.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<BusinessOrderDetailResponse?> GetOrderAsync(
        Guid businessId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.BusinessId == businessId)
            .Select(o => new BusinessOrderDetailResponse
            {
                Id = o.Id,
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                CustomerPhone = o.CustomerPhone,
                ShippingAddressLine1 = o.ShippingAddressLine1,
                ShippingAddressLine2 = o.ShippingAddressLine2,
                ShippingCity = o.ShippingCity,
                ShippingState = o.ShippingState,
                ShippingPostalCode = o.ShippingPostalCode,
                ShippingCountry = o.ShippingCountry,
                CustomerNotes = o.CustomerNotes,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                Subtotal = o.Subtotal,
                Total = o.Total,
                Currency = o.Currency,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                Items = o.Items.Select(i => new BusinessOrderItemResponse
                {
                    ProductId = i.ProductId,
                    ProductTitle = i.ProductTitle,
                    ProductImageUrl = i.ProductImageUrl,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal,
                }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Order?> GetTrackedOrderAsync(
        Guid businessId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.BusinessId == businessId, cancellationToken);
    }

    public async Task UpdateOrderStatusAsync(
        Order order,
        OrderStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = DateTime.UtcNow;

            // Cancelling reverses CreateOrderAsync's decrement — same StockMovement
            // shape, positive Amount this time, so the ledger reads as a clean pair of
            // opposite entries rather than a gap.
            if (newStatus == OrderStatus.Cancelled)
            {
                var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();

                var products = await _db.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

                var shortRef = order.Id.ToString("N")[..8];

                foreach (var item in order.Items)
                {
                    if (!products.TryGetValue(item.ProductId, out var product) || product.StockQuantity is null)
                    {
                        continue;
                    }

                    var newStock = product.StockQuantity.Value + item.Quantity;
                    product.StockQuantity = newStock;
                    product.UpdatedAt = now;

                    await _db.StockMovements.AddAsync(
                        new StockMovement
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            BusinessId = order.BusinessId,
                            Amount = item.Quantity,
                            BalanceAfter = newStock,
                            Reason = $"Order #{shortRef} cancelled",
                            CreatedByUserId = Guid.Empty,
                            CreatedAt = now,
                        },
                        cancellationToken);
                }
            }

            order.Status = newStatus;
            order.UpdatedAt = now;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateOrderPaymentStatusAsync(
        Order order,
        PaymentStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        order.PaymentStatus = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountOrdersAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _db.Orders.CountAsync(o => o.BusinessId == businessId, cancellationToken);
    }

    public async Task<int> CountOrdersByStatusAsync(
        Guid businessId,
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _db.Orders.CountAsync(o => o.BusinessId == businessId && o.Status == status, cancellationToken);
    }

    public async Task<bool> HasOrderItemsForProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _db.OrderItems.AnyAsync(i => i.ProductId == productId, cancellationToken);
    }
}
