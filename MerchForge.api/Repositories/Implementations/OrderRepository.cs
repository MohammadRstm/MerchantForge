using MerchForge.api.Data;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Orders;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations;

public class OrderRepository : IOrderRepository
{
    private readonly MerchForgeDbContext _db;
    private readonly IProductImageUrlResolver _productImageUrlResolver;

    public OrderRepository(
        MerchForgeDbContext db,
        IProductImageUrlResolver productImageUrlResolver)
    {
        _db = db;
        _productImageUrlResolver = productImageUrlResolver;
    }

    // ---- storefront ----

    public async Task<Order> CreateOrderAsync(
        Guid businessId,
        CreateOrderRequest request,
        Guid? customerId,
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
                CustomerId = customerId,
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

            await _db.OrderStatusHistories.AddAsync(
                new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Status = OrderStatus.Pending,
                    // No dashboard user placed this order — same Guid.Empty sentinel
                    // the stock movements above use for customer/system-driven rows.
                    ChangedByUserId = Guid.Empty,
                    CreatedAt = now,
                },
                cancellationToken);

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
        var order = await _db.Orders
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

        if (order is not null)
        {
            foreach (var item in order.Items)
            {
                item.ProductImageUrl = _productImageUrlResolver.ToPublicUrl(item.ProductImageUrl);
            }
        }

        return order;
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
                EF.Functions.Like(o.CustomerName, pattern) ||
                EF.Functions.Like(o.CustomerEmail, pattern) ||
                (o.CustomerPhone != null && EF.Functions.Like(o.CustomerPhone, pattern)) ||
                o.Items.Any(i => EF.Functions.Like(i.ProductTitle, pattern)));
        }

        if (query.From.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAt <= query.To.Value);
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
                CustomerPhone = o.CustomerPhone,
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
        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.BusinessId == businessId)
            .Select(o => new BusinessOrderDetailResponse
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
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
                CustomerOrderCount = o.CustomerId == null
                    ? null
                    : _db.Orders.Count(o2 => o2.BusinessId == businessId && o2.CustomerId == o.CustomerId),
                CustomerLastOrderAt = o.CustomerId == null
                    ? null
                    : _db.Orders
                        .Where(o2 => o2.BusinessId == businessId && o2.CustomerId == o.CustomerId && o2.Id != o.Id)
                        .Max(o2 => (DateTime?)o2.CreatedAt),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is not null)
        {
            foreach (var item in order.Items)
            {
                item.ProductImageUrl = _productImageUrlResolver.ToPublicUrl(item.ProductImageUrl);
            }
        }

        return order;
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
        Guid changedByUserId,
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

            await _db.OrderStatusHistories.AddAsync(
                new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Status = newStatus,
                    ChangedByUserId = changedByUserId,
                    CreatedAt = now,
                },
                cancellationToken);

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

    public async Task<OrderStatsResponse> GetOrderStatsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var staleCutoff = DateTime.UtcNow.AddHours(-24);

        var counts = await _db.Orders
            .Where(o => o.BusinessId == businessId)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var countsByStatus = counts.ToDictionary(c => c.Status, c => c.Count);

        var oldestPendingOrderCreatedAt = await _db.Orders
            .Where(o => o.BusinessId == businessId && o.Status == OrderStatus.Pending)
            .OrderBy(o => o.CreatedAt)
            .Select(o => (DateTime?)o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var stalePendingCount = await _db.Orders.CountAsync(
            o => o.BusinessId == businessId && o.Status == OrderStatus.Pending && o.CreatedAt <= staleCutoff,
            cancellationToken);

        var recentlyCancelledCount = await _db.Orders.CountAsync(
            o => o.BusinessId == businessId && o.Status == OrderStatus.Cancelled && o.UpdatedAt >= staleCutoff,
            cancellationToken);

        return new OrderStatsResponse
        {
            TotalCount = countsByStatus.Values.Sum(),
            PendingCount = countsByStatus.GetValueOrDefault(OrderStatus.Pending),
            ConfirmedCount = countsByStatus.GetValueOrDefault(OrderStatus.Confirmed),
            ShippedCount = countsByStatus.GetValueOrDefault(OrderStatus.Shipped),
            DeliveredCount = countsByStatus.GetValueOrDefault(OrderStatus.Delivered),
            CancelledCount = countsByStatus.GetValueOrDefault(OrderStatus.Cancelled),
            StalePendingCount = stalePendingCount,
            OldestPendingOrderCreatedAt = oldestPendingOrderCreatedAt,
            RecentlyCancelledCount = recentlyCancelledCount,
        };
    }

    public async Task<List<OrderNoteResponse>> GetOrderNotesAsync(
        Guid businessId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Orders.AnyAsync(o => o.Id == orderId && o.BusinessId == businessId, cancellationToken))
        {
            throw new OrderNotFoundException();
        }

        return await _db.OrderNotes
            .AsNoTracking()
            .Where(n => n.OrderId == orderId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new OrderNoteResponse
            {
                Id = n.Id,
                Content = n.Content,
                CreatedByUserName = _db.Users
                    .Where(u => u.Id == n.CreatedByUserId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault() ?? "Unknown",
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderNoteResponse> AddOrderNoteAsync(
        Guid businessId,
        Guid orderId,
        string content,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Orders.AnyAsync(o => o.Id == orderId && o.BusinessId == businessId, cancellationToken))
        {
            throw new OrderNotFoundException();
        }

        var note = new OrderNote
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Content = content.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };

        await _db.OrderNotes.AddAsync(note, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == createdByUserId, cancellationToken);

        return new OrderNoteResponse
        {
            Id = note.Id,
            Content = note.Content,
            CreatedByUserName = user is null ? "Unknown" : $"{user.FirstName} {user.LastName}",
            CreatedAt = note.CreatedAt,
        };
    }

    public async Task<List<OrderStatusHistoryEntryResponse>> GetOrderStatusHistoryAsync(
        Guid businessId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Orders.AnyAsync(o => o.Id == orderId && o.BusinessId == businessId, cancellationToken))
        {
            throw new OrderNotFoundException();
        }

        return await _db.OrderStatusHistories
            .AsNoTracking()
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new OrderStatusHistoryEntryResponse
            {
                Status = h.Status,
                ChangedByUserName = h.ChangedByUserId == Guid.Empty
                    ? null
                    : _db.Users
                        .Where(u => u.Id == h.ChangedByUserId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault(),
                CreatedAt = h.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderAnalyticsResponse> GetOrderAnalyticsAsync(
        Guid businessId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        // Excludes Cancelled everywhere here - a cancelled order contributes no real
        // revenue, and counting it as "order volume" would make the Orders trend
        // diverge from the Revenue trend for a reason the chart can't explain.
        var granularity = (to - from).TotalDays <= 31
            ? OrderAnalyticsGranularity.Daily
            : OrderAnalyticsGranularity.Monthly;

        var baseQuery = _db.Orders.Where(o =>
            o.BusinessId == businessId &&
            o.Status != OrderStatus.Cancelled &&
            o.CreatedAt >= from &&
            o.CreatedAt <= to);

        // One GROUP BY query either way - only what the bucket key looks like differs.
        List<OrderAnalyticsPointResponse> points;

        if (granularity == OrderAnalyticsGranularity.Daily)
        {
            points = await baseQuery
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new OrderAnalyticsPointResponse
                {
                    Period = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.Total),
                })
                .OrderBy(p => p.Period)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Reconstructing a DateTime from g.Key.Year/g.Key.Month inside the
            // server-evaluated Select isn't translatable by the MySQL provider - it
            // materializes as raw (Year, Month) instead, and new DateTime(...) only
            // happens client-side afterward, on what's already a small, in-memory list.
            var monthlyBuckets = await baseQuery
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.Total),
                })
                .ToListAsync(cancellationToken);

            points = monthlyBuckets
                .Select(b => new OrderAnalyticsPointResponse
                {
                    Period = new DateTime(b.Year, b.Month, 1),
                    OrderCount = b.OrderCount,
                    Revenue = b.Revenue,
                })
                .OrderBy(p => p.Period)
                .ToList();
        }

        // Summed from the already-fetched buckets rather than a second query - the
        // buckets exactly partition [from, to], so this is exact, not an estimate.
        var currentTotals = new OrderAnalyticsPeriodTotalsResponse
        {
            OrderCount = points.Sum(p => p.OrderCount),
            Revenue = points.Sum(p => p.Revenue),
        };

        // The equal-length window immediately preceding [from, to], with no overlap.
        var span = to - from;
        var previousTo = from.AddTicks(-1);
        var previousFrom = previousTo - span;

        var previousTotals = await _db.Orders
            .Where(o =>
                o.BusinessId == businessId &&
                o.Status != OrderStatus.Cancelled &&
                o.CreatedAt >= previousFrom &&
                o.CreatedAt <= previousTo)
            .GroupBy(o => 1)
            .Select(g => new OrderAnalyticsPeriodTotalsResponse
            {
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.Total),
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new OrderAnalyticsPeriodTotalsResponse();

        return new OrderAnalyticsResponse
        {
            Granularity = granularity,
            Points = points,
            CurrentPeriod = currentTotals,
            PreviousPeriod = previousTotals,
            // Null rather than a percentage against zero - see the DTO's own doc comment.
            RevenueChangePercent = previousTotals.Revenue > 0
                ? Math.Round((currentTotals.Revenue - previousTotals.Revenue) / previousTotals.Revenue * 100, 1)
                : null,
            OrderCountChangePercent = previousTotals.OrderCount > 0
                ? Math.Round(
                    (decimal)(currentTotals.OrderCount - previousTotals.OrderCount) / previousTotals.OrderCount * 100,
                    1)
                : null,
        };
    }

    public async Task<ProductAnalyticsResponse> GetProductAnalyticsAsync(
        Guid businessId,
        DateTime from,
        DateTime to,
        Guid? productId,
        CancellationToken cancellationToken = default)
    {
        var granularity = (to - from).TotalDays <= 31
            ? OrderAnalyticsGranularity.Daily
            : OrderAnalyticsGranularity.Monthly;

        var baseQuery = _db.OrderItems.Where(i =>
            i.Order.BusinessId == businessId &&
            i.Order.Status != OrderStatus.Cancelled &&
            i.Order.CreatedAt >= from &&
            i.Order.CreatedAt <= to &&
            (productId == null || i.ProductId == productId));

        List<ProductAnalyticsPointResponse> points;

        if (granularity == OrderAnalyticsGranularity.Daily)
        {
            points = await baseQuery
                .GroupBy(i => i.Order.CreatedAt.Date)
                .Select(g => new ProductAnalyticsPointResponse
                {
                    Period = g.Key,
                    Revenue = g.Sum(i => i.LineTotal),
                    UnitsSold = g.Sum(i => i.Quantity),
                    OrderCount = g.Select(i => i.OrderId).Distinct().Count(),
                })
                .OrderBy(p => p.Period)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Same fix as GetOrderAnalyticsAsync: reconstructing a DateTime from
            // g.Key.Year/g.Key.Month inside the server-evaluated Select isn't
            // translatable by the MySQL provider, so the (Year, Month) pair is
            // materialized first and the DateTime built client-side afterward.
            var monthlyBuckets = await baseQuery
                .GroupBy(i => new { i.Order.CreatedAt.Year, i.Order.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(i => i.LineTotal),
                    UnitsSold = g.Sum(i => i.Quantity),
                    OrderCount = g.Select(i => i.OrderId).Distinct().Count(),
                })
                .ToListAsync(cancellationToken);

            points = monthlyBuckets
                .Select(b => new ProductAnalyticsPointResponse
                {
                    Period = new DateTime(b.Year, b.Month, 1),
                    Revenue = b.Revenue,
                    UnitsSold = b.UnitsSold,
                    OrderCount = b.OrderCount,
                })
                .OrderBy(p => p.Period)
                .ToList();
        }

        // Summed from the already-fetched buckets, not a second query - they exactly
        // partition [from, to]. OrderCount is the one field this can't do exactly
        // right for (an order spanning two buckets - impossible here since a bucket
        // is a whole day/month - or containing items from two different bucket-worthy
        // dates, which can't happen either), so summing bucket order counts is exact.
        var currentTotals = new ProductAnalyticsPeriodTotalsResponse
        {
            Revenue = points.Sum(p => p.Revenue),
            UnitsSold = points.Sum(p => p.UnitsSold),
            OrderCount = points.Sum(p => p.OrderCount),
        };

        var span = to - from;
        var previousTo = from.AddTicks(-1);
        var previousFrom = previousTo - span;

        var previousTotals = await GetProductPeriodTotalsAsync(businessId, previousFrom, previousTo, productId, cancellationToken);

        ProductAllTimeTotalsResponse? allTime = null;

        if (productId.HasValue)
        {
            var allTimeTotals = await GetProductPeriodTotalsAsync(
                businessId, DateTime.MinValue, DateTime.MaxValue, productId, cancellationToken);

            allTime = new ProductAllTimeTotalsResponse
            {
                Revenue = allTimeTotals.Revenue,
                UnitsSold = allTimeTotals.UnitsSold,
                OrderCount = allTimeTotals.OrderCount,
                AverageUnitsPerOrder = allTimeTotals.OrderCount > 0
                    ? Math.Round((decimal)allTimeTotals.UnitsSold / allTimeTotals.OrderCount, 1)
                    : null,
            };
        }

        return new ProductAnalyticsResponse
        {
            Granularity = granularity,
            Points = points,
            CurrentPeriod = currentTotals,
            PreviousPeriod = previousTotals,
            RevenueChangePercent = previousTotals.Revenue > 0
                ? Math.Round((currentTotals.Revenue - previousTotals.Revenue) / previousTotals.Revenue * 100, 1)
                : null,
            UnitsSoldChangePercent = previousTotals.UnitsSold > 0
                ? Math.Round(
                    (decimal)(currentTotals.UnitsSold - previousTotals.UnitsSold) / previousTotals.UnitsSold * 100, 1)
                : null,
            OrderCountChangePercent = previousTotals.OrderCount > 0
                ? Math.Round(
                    (decimal)(currentTotals.OrderCount - previousTotals.OrderCount) / previousTotals.OrderCount * 100,
                    1)
                : null,
            AllTime = allTime,
        };
    }

    private async Task<ProductAnalyticsPeriodTotalsResponse> GetProductPeriodTotalsAsync(
        Guid businessId,
        DateTime from,
        DateTime to,
        Guid? productId,
        CancellationToken cancellationToken)
    {
        var result = await _db.OrderItems
            .Where(i =>
                i.Order.BusinessId == businessId &&
                i.Order.Status != OrderStatus.Cancelled &&
                i.Order.CreatedAt >= from &&
                i.Order.CreatedAt <= to &&
                (productId == null || i.ProductId == productId))
            .GroupBy(i => 1)
            .Select(g => new ProductAnalyticsPeriodTotalsResponse
            {
                Revenue = g.Sum(i => i.LineTotal),
                UnitsSold = g.Sum(i => i.Quantity),
                OrderCount = g.Select(i => i.OrderId).Distinct().Count(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result ?? new ProductAnalyticsPeriodTotalsResponse();
    }

    public async Task<(int UnitsSold, decimal Revenue)> GetAllTimeProductSalesTotalsAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var totals = await GetProductPeriodTotalsAsync(
            businessId, DateTime.MinValue, DateTime.MaxValue, null, cancellationToken);

        return (totals.UnitsSold, totals.Revenue);
    }

    public async Task<CustomerSnapshotResponse> GetCustomerSnapshotAsync(
        Guid businessId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        // Excludes Cancelled, matching every other analytics query here.
        var baseQuery = _db.Orders.Where(o => o.BusinessId == businessId && o.Status != OrderStatus.Cancelled);

        var totalCustomers = await baseQuery
            .Select(o => o.CustomerEmail)
            .Distinct()
            .CountAsync(cancellationToken);

        // A HAVING clause on each email's earliest order date - stays a single
        // server-side aggregate rather than pulling every customer's first-order
        // date into memory to filter client-side.
        var newCustomersInPeriod = await baseQuery
            .GroupBy(o => o.CustomerEmail)
            .Where(g => g.Min(o => o.CreatedAt) >= from && g.Min(o => o.CreatedAt) <= to)
            .CountAsync(cancellationToken);

        return new CustomerSnapshotResponse
        {
            TotalCustomers = totalCustomers,
            NewCustomersInPeriod = newCustomersInPeriod,
        };
    }
}
