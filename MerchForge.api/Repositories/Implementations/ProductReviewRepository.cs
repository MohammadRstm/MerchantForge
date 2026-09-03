using MerchForge.api.Data;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class ProductReviewRepository : IProductReviewRepository
    {
        private readonly MerchForgeDbContext _db;

        public ProductReviewRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<(List<StorefrontProductReviewResponse> Items, int TotalCount)> GetVisibleReviewsAsync(
            Guid businessId,
            Guid productId,
            ProductReviewsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var baseQuery = VisibleReviews(businessId, productId);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Id is a stable tie-breaker: without it, paging over reviews sharing a
            // CreatedAt can repeat or skip rows between pages — same reasoning as
            // StorefrontRepository's product sort.
            // The author's two name parts are fetched raw and combined in memory below.
            // BuildDisplayName is a plain C# method that EF can't translate into SQL, so
            // calling it inside this projection would throw at query time.
            var rows = await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .ThenBy(r => r.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.Customer.FirstName,
                    r.Customer.LastName,
                    r.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var items = rows
                .Select(r => new StorefrontProductReviewResponse
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    AuthorDisplayName = BuildDisplayName(r.FirstName, r.LastName),
                    CreatedAt = r.CreatedAt,
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<ProductReviewSummaryResponse> GetSummaryAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            // One grouped round trip for the whole histogram rather than five counts:
            // the average and the total both fall out of these same rows in C#.
            var counts = await VisibleReviews(businessId, productId)
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            // Always all five keys, zeros included, so a storefront can render the full
            // breakdown without filling gaps itself.
            var breakdown = Enumerable.Range(1, 5)
                .ToDictionary(
                    rating => rating,
                    rating => counts.FirstOrDefault(c => c.Rating == rating)?.Count ?? 0);

            var reviewCount = counts.Sum(c => c.Count);

            return new ProductReviewSummaryResponse
            {
                // Null rather than 0 for "no reviews yet" — a real average can never be
                // 0, so 0 would be indistinguishable from genuinely terrible.
                AverageRating = reviewCount == 0
                    ? null
                    : Math.Round(
                        (decimal)counts.Sum(c => c.Rating * c.Count) / reviewCount,
                        2,
                        MidpointRounding.AwayFromZero),
                ReviewCount = reviewCount,
                RatingBreakdown = breakdown,
            };
        }

        public async Task<(List<OwnerProductReviewResponse> Items, int TotalCount)> GetReviewsForOwnerAsync(
            Guid businessId,
            Guid productId,
            ProductReviewsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            // No IsHidden filter, unlike the storefront read: hiding a review must not
            // hide it from the owner who hid it, or unhiding would be unreachable.
            var baseQuery = _db.ProductReviews
                .AsNoTracking()
                .Where(r => r.BusinessId == businessId && r.ProductId == productId);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .ThenBy(r => r.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(r => new OwnerProductReviewResponse
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CustomerName = (r.Customer.FirstName + " " + r.Customer.LastName).Trim(),
                    CustomerEmail = r.Customer.Email,
                    IsHidden = r.IsHidden,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<ProductReview?> GetByProductAndCustomerAsync(
            Guid businessId,
            Guid productId,
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            // Tracked: this is the read half of the upsert, so the caller mutates and
            // saves the same instance.
            return await _db.ProductReviews
                .FirstOrDefaultAsync(
                    r => r.BusinessId == businessId
                        && r.ProductId == productId
                        && r.CustomerId == customerId,
                    cancellationToken);
        }

        public async Task<bool> HasPurchasedProductAsync(
            Guid businessId,
            Guid productId,
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            // Cancelled is the only status that doesn't count, matching how
            // DashboardRepository and OrderRepository already decide what a real order
            // is. PaymentStatus is deliberately not consulted: there is no payment
            // gateway yet, so gating on it would refuse every legitimate reviewer.
            return await _db.OrderItems
                .AsNoTracking()
                .AnyAsync(
                    i => i.ProductId == productId
                        && i.Order.CustomerId == customerId
                        && i.Order.BusinessId == businessId
                        && i.Order.Status != OrderStatus.Cancelled,
                    cancellationToken);
        }

        public async Task<ProductReview?> GetForOwnerAsync(
            Guid businessId,
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            return await _db.ProductReviews
                .FirstOrDefaultAsync(
                    r => r.Id == reviewId && r.BusinessId == businessId,
                    cancellationToken);
        }

        public async Task AddAsync(ProductReview review, CancellationToken cancellationToken = default)
        {
            await _db.ProductReviews.AddAsync(review, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// The storefront's view of a product's reviews. Every public read path goes
        /// through this so the IsHidden filter can't be forgotten in one of them.
        /// </summary>
        private IQueryable<ProductReview> VisibleReviews(Guid businessId, Guid productId)
        {
            return _db.ProductReviews
                .AsNoTracking()
                .Where(r => r.BusinessId == businessId
                    && r.ProductId == productId
                    && !r.IsHidden);
        }

        /// <summary>
        /// First name plus last initial, e.g. "Mia S." — enough to feel like a person
        /// wrote it without publishing customers' full names on a public page. Applied
        /// in memory after the query, since EF can't translate it into SQL.
        /// </summary>
        private static string BuildDisplayName(string firstName, string lastName)
        {
            return string.IsNullOrWhiteSpace(firstName)
                ? "Customer"
                : string.IsNullOrWhiteSpace(lastName)
                    ? firstName
                    : firstName + " " + lastName.Substring(0, 1) + ".";
        }
    }
}
