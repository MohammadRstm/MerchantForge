using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.ProductReviews.interfaces;

namespace MerchForge.api.Services.ProductReviews
{
    public class ProductReviewService : IProductReviewService
    {
        private readonly IProductReviewRepository _productReviewRepository;
        private readonly IStorefrontRepository _storefrontRepository;

        public ProductReviewService(
            IProductReviewRepository productReviewRepository,
            IStorefrontRepository storefrontRepository)
        {
            _productReviewRepository = productReviewRepository;
            _storefrontRepository = storefrontRepository;
        }

        public async Task<PagedResult<StorefrontProductReviewResponse>> GetVisibleReviewsAsync(
            Guid businessId,
            Guid productId,
            ProductReviewsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            await EnsureProductExistsAsync(businessId, productId, cancellationToken);

            var (items, totalCount) = await _productReviewRepository.GetVisibleReviewsAsync(
                businessId,
                productId,
                query,
                cancellationToken);

            return new PagedResult<StorefrontProductReviewResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<ProductReviewSummaryResponse> GetSummaryAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            await EnsureProductExistsAsync(businessId, productId, cancellationToken);

            return await _productReviewRepository.GetSummaryAsync(businessId, productId, cancellationToken);
        }

        public async Task<ProductReviewEligibilityResponse> GetEligibilityAsync(
            Guid businessId,
            Guid productId,
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            await EnsureProductExistsAsync(businessId, productId, cancellationToken);

            var existing = await _productReviewRepository.GetByProductAndCustomerAsync(
                businessId,
                productId,
                customerId,
                cancellationToken);

            // A customer who already has a review necessarily qualified when they wrote
            // it, so the purchase check is skipped for them — it would be a wasted query
            // that can only ever say yes.
            var canReview = existing is not null
                || await _productReviewRepository.HasPurchasedProductAsync(
                    businessId,
                    productId,
                    customerId,
                    cancellationToken);

            return new ProductReviewEligibilityResponse
            {
                CanReview = canReview,
                MyReview = existing is null ? null : ToMyReview(existing),
            };
        }

        public async Task<MyProductReviewResponse> SubmitReviewAsync(
            Guid businessId,
            Guid productId,
            Guid customerId,
            CreateProductReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            await EnsureProductExistsAsync(businessId, productId, cancellationToken);

            var existing = await _productReviewRepository.GetByProductAndCustomerAsync(
                businessId,
                productId,
                customerId,
                cancellationToken);

            // Only checked when creating. An existing review already proves the customer
            // qualified once, and a business shouldn't be able to strand somebody's
            // review as uneditable by, say, cancelling the order after the fact.
            if (existing is null)
            {
                var hasPurchased = await _productReviewRepository.HasPurchasedProductAsync(
                    businessId,
                    productId,
                    customerId,
                    cancellationToken);

                if (!hasPurchased)
                {
                    throw new ReviewRequiresPurchaseException();
                }
            }

            var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

            if (existing is null)
            {
                existing = new ProductReview
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    ProductId = productId,
                    CustomerId = customerId,
                    Rating = request.Rating,
                    Comment = comment,
                    IsHidden = false,
                };

                await _productReviewRepository.AddAsync(existing, cancellationToken);
            }
            else
            {
                existing.Rating = request.Rating;
                existing.Comment = comment;
                existing.UpdatedAt = DateTime.UtcNow;

                // IsHidden is deliberately not reset. Editing a hidden review must not
                // be a way to put it back on the storefront over the owner's decision.
            }

            await _productReviewRepository.SaveChangesAsync(cancellationToken);

            return ToMyReview(existing);
        }

        public async Task<PagedResult<OwnerProductReviewResponse>> GetReviewsForOwnerAsync(
            Guid businessId,
            Guid productId,
            ProductReviewsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            await EnsureProductExistsAsync(businessId, productId, cancellationToken);

            var (items, totalCount) = await _productReviewRepository.GetReviewsForOwnerAsync(
                businessId,
                productId,
                query,
                cancellationToken);

            return new PagedResult<OwnerProductReviewResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task SetReviewVisibilityAsync(
            Guid businessId,
            Guid reviewId,
            bool isHidden,
            CancellationToken cancellationToken = default)
        {
            var review = await _productReviewRepository.GetForOwnerAsync(businessId, reviewId, cancellationToken)
                ?? throw new ProductReviewNotFoundException();

            if (review.IsHidden == isHidden)
            {
                return;
            }

            review.IsHidden = isHidden;
            review.UpdatedAt = DateTime.UtcNow;

            await _productReviewRepository.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Both "no such product" and "that product belongs to another business" raise
        /// the same ProductNotFoundException, so review endpoints can't be used to probe
        /// which product ids exist under a business other than the one being asked
        /// about — the same reasoning that exception already documents.
        /// </summary>
        private async Task EnsureProductExistsAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken)
        {
            var exists = await _storefrontRepository.ProductExistsAsync(businessId, productId, cancellationToken);

            if (!exists)
            {
                throw new ProductNotFoundException();
            }
        }

        private static MyProductReviewResponse ToMyReview(ProductReview review)
        {
            return new MyProductReviewResponse
            {
                Id = review.Id,
                Rating = review.Rating,
                Comment = review.Comment,
                IsHidden = review.IsHidden,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
            };
        }
    }
}
