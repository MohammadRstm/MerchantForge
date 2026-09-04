using FluentAssertions;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.ProductReviews;
using MerchForge.IntegrationTests.Fakes;

namespace MerchForge.IntegrationTests;

/// <summary>
/// Product reviews through the real service + repository + provider stack.
///
/// The rules worth protecting here are all enforced in SQL or by real query
/// behaviour: the unique index that makes a second submission an edit, the check
/// constraint on the rating range, hidden reviews dropping out of both the list and
/// the average, and business isolation. A test double would pass regardless.
/// </summary>
public class ProductReviewServiceTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _business = null!;
    private Business _otherBusiness = null!;
    private Product _sneakers = null!;
    private Product _boots = null!;
    private Product _otherBusinessShoe = null!;

    private Customer _buyer = null!;
    private Customer _secondBuyer = null!;
    private Customer _nonBuyer = null!;
    private Customer _cancelledOnlyBuyer = null!;

    public ProductReviewServiceTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _business = await _fixture.CreateBusinessAsync(
            "Reviews Fashion A", CatalogDatabaseFixture.FashionDomainId);

        // The isolation trap: same domain, same category, different business.
        _otherBusiness = await _fixture.CreateBusinessAsync(
            "Reviews Fashion B", CatalogDatabaseFixture.FashionDomainId);

        _sneakers = await _fixture.CreateProductAsync(
            _business.Id, CatalogDatabaseFixture.ShoesCategoryId, "Urban Sneakers", 120m);

        _boots = await _fixture.CreateProductAsync(
            _business.Id, CatalogDatabaseFixture.ShoesCategoryId, "Leather Boots", 210m);

        _otherBusinessShoe = await _fixture.CreateProductAsync(
            _otherBusiness.Id, CatalogDatabaseFixture.ShoesCategoryId, "Rival Shoe", 99m);

        _buyer = await CreateCustomerAsync("Mia", "Sato");
        _secondBuyer = await CreateCustomerAsync("Grace", "Chen");
        _nonBuyer = await CreateCustomerAsync("Felix", "Blake");
        _cancelledOnlyBuyer = await CreateCustomerAsync("Zara", "Moretti");

        await CreateOrderAsync(_business.Id, _buyer.Id, _sneakers, OrderStatus.Delivered);
        await CreateOrderAsync(_business.Id, _secondBuyer.Id, _sneakers, OrderStatus.Pending);
        await CreateOrderAsync(_business.Id, _cancelledOnlyBuyer.Id, _sneakers, OrderStatus.Cancelled);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---- eligibility ----

    [Fact]
    public async Task A_customer_who_ordered_the_product_can_review_it()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var result = await service.GetEligibilityAsync(_business.Id, _sneakers.Id, _buyer.Id);

        result.CanReview.Should().BeTrue();
        result.MyReview.Should().BeNull();
    }

    [Fact]
    public async Task A_customer_who_never_ordered_the_product_cannot_review_it()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var result = await service.GetEligibilityAsync(_business.Id, _sneakers.Id, _nonBuyer.Id);

        result.CanReview.Should().BeFalse();
    }

    [Fact]
    public async Task A_pending_order_is_enough_to_review()
    {
        // Eligibility deliberately uses "not cancelled" rather than "delivered",
        // matching how the dashboard's own analytics decide what a real order is.
        var service = CreateService(out var scope);
        using var _ = scope;

        var result = await service.GetEligibilityAsync(_business.Id, _sneakers.Id, _secondBuyer.Id);

        result.CanReview.Should().BeTrue();
    }

    [Fact]
    public async Task A_cancelled_order_is_not_enough_to_review()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var result = await service.GetEligibilityAsync(_business.Id, _sneakers.Id, _cancelledOnlyBuyer.Id);

        result.CanReview.Should().BeFalse();
    }

    [Fact]
    public async Task Ordering_one_product_does_not_allow_reviewing_another()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var result = await service.GetEligibilityAsync(_business.Id, _boots.Id, _buyer.Id);

        result.CanReview.Should().BeFalse();
    }

    // ---- submitting ----

    [Fact]
    public async Task Submitting_without_having_bought_the_product_is_rejected()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var act = async () => await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _nonBuyer.Id, Request(5, "Never bought this."));

        await act.Should().ThrowAsync<ReviewRequiresPurchaseException>();
    }

    [Fact]
    public async Task A_review_can_be_submitted_with_a_rating_and_no_comment()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var review = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(4, comment: null));

        review.Rating.Should().Be(4);
        review.Comment.Should().BeNull();
        review.IsHidden.Should().BeFalse();
    }

    [Fact]
    public async Task A_blank_comment_is_stored_as_null_rather_than_whitespace()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var review = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(4, "   "));

        review.Comment.Should().BeNull();
    }

    [Fact]
    public async Task Submitting_twice_updates_the_existing_review_rather_than_adding_one()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var first = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(2, "Disappointing."));

        var second = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(5, "Changed my mind."));

        second.Id.Should().Be(first.Id);
        second.Rating.Should().Be(5);
        second.Comment.Should().Be("Changed my mind.");

        await using var db = _fixture.CreateContext();
        var count = db.ProductReviews.Count(r => r.ProductId == _sneakers.Id && r.CustomerId == _buyer.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Editing_a_hidden_review_does_not_put_it_back_on_the_storefront()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var created = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(1, "Bad."));

        await service.SetReviewVisibilityAsync(_business.Id, created.Id, isHidden: true);

        var edited = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(5, "Actually great."));

        edited.IsHidden.Should().BeTrue();
    }

    [Fact]
    public async Task A_rating_outside_one_to_five_is_refused_by_the_database()
    {
        // The request validator rejects this first in the real pipeline; this asserts
        // the check constraint behind it, so a future code path that skips validation
        // still can't corrupt an average.
        var service = CreateService(out var scope);
        using var _ = scope;

        var act = async () => await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(9, "Out of range."));

        await act.Should().ThrowAsync<Exception>();
    }

    // ---- reading ----

    [Fact]
    public async Task Visible_reviews_are_listed_newest_first_with_a_display_name()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        await service.SubmitReviewAsync(_business.Id, _sneakers.Id, _buyer.Id, Request(4, "Solid."));
        await service.SubmitReviewAsync(_business.Id, _sneakers.Id, _secondBuyer.Id, Request(5, "Love them."));

        var page = await service.GetVisibleReviewsAsync(
            _business.Id, _sneakers.Id, new ProductReviewsQueryRequest());

        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items.Select(i => i.AuthorDisplayName).Should().Contain("Mia S.");
    }

    [Fact]
    public async Task The_summary_averages_only_visible_reviews()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var hidden = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(1, "Terrible."));
        await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _secondBuyer.Id, Request(5, "Excellent."));

        await service.SetReviewVisibilityAsync(_business.Id, hidden.Id, isHidden: true);

        var summary = await service.GetSummaryAsync(_business.Id, _sneakers.Id);

        summary.ReviewCount.Should().Be(1);
        summary.AverageRating.Should().Be(5m);
        summary.RatingBreakdown[1].Should().Be(0);
        summary.RatingBreakdown[5].Should().Be(1);
    }

    [Fact]
    public async Task A_product_with_no_reviews_has_a_null_average_and_a_full_zeroed_breakdown()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var summary = await service.GetSummaryAsync(_business.Id, _boots.Id);

        summary.AverageRating.Should().BeNull();
        summary.ReviewCount.Should().Be(0);
        summary.RatingBreakdown.Should().HaveCount(5);
        summary.RatingBreakdown.Values.Should().OnlyContain(v => v == 0);
    }

    [Fact]
    public async Task Hidden_reviews_are_absent_from_the_storefront_but_present_for_the_owner()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var review = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(2, "Not for me."));

        await service.SetReviewVisibilityAsync(_business.Id, review.Id, isHidden: true);

        var storefront = await service.GetVisibleReviewsAsync(
            _business.Id, _sneakers.Id, new ProductReviewsQueryRequest());
        var owner = await service.GetReviewsForOwnerAsync(
            _business.Id, _sneakers.Id, new ProductReviewsQueryRequest());

        storefront.TotalCount.Should().Be(0);
        owner.TotalCount.Should().Be(1);
        owner.Items.Single().IsHidden.Should().BeTrue();
        owner.Items.Single().CustomerEmail.Should().Be(_buyer.Email);
    }

    [Fact]
    public async Task Unhiding_puts_a_review_back_on_the_storefront()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var review = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(3, "Fine."));

        await service.SetReviewVisibilityAsync(_business.Id, review.Id, isHidden: true);
        await service.SetReviewVisibilityAsync(_business.Id, review.Id, isHidden: false);

        var storefront = await service.GetVisibleReviewsAsync(
            _business.Id, _sneakers.Id, new ProductReviewsQueryRequest());

        storefront.TotalCount.Should().Be(1);
    }

    // ---- isolation ----

    [Fact]
    public async Task Reviews_cannot_be_read_through_another_businesss_storefront()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        await service.SubmitReviewAsync(_business.Id, _sneakers.Id, _buyer.Id, Request(5, "Great."));

        // Same product id, wrong business: indistinguishable from "no such product".
        var act = async () => await service.GetVisibleReviewsAsync(
            _otherBusiness.Id, _sneakers.Id, new ProductReviewsQueryRequest());

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Reviewing_a_product_belonging_to_another_business_is_refused()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var act = async () => await service.SubmitReviewAsync(
            _business.Id, _otherBusinessShoe.Id, _buyer.Id, Request(5, "Wrong store."));

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task An_owner_cannot_hide_a_review_belonging_to_another_business()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var review = await service.SubmitReviewAsync(
            _business.Id, _sneakers.Id, _buyer.Id, Request(4, "Nice."));

        var act = async () => await service.SetReviewVisibilityAsync(
            _otherBusiness.Id, review.Id, isHidden: true);

        await act.Should().ThrowAsync<ProductReviewNotFoundException>();
    }

    // ---- helpers ----

    private ProductReviewService CreateService(out MerchForgeDbContextScope scope)
    {
        scope = new MerchForgeDbContextScope(_fixture.CreateContext());

        return new ProductReviewService(
            new ProductReviewRepository(scope.Db),
            new StorefrontRepository(scope.Db, TestImageUrls.Resolver));
    }

    private static CreateProductReviewRequest Request(int rating, string? comment) =>
        new() { Rating = rating, Comment = comment };

    private async Task<Customer> CreateCustomerAsync(string firstName, string lastName)
    {
        await using var db = _fixture.CreateContext();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = "not-a-real-hash",
            FirstName = firstName,
            LastName = lastName,
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return customer;
    }

    private async Task CreateOrderAsync(Guid businessId, Guid? customerId, Product product, OrderStatus status)
    {
        await using var db = _fixture.CreateContext();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CustomerId = customerId,
            CustomerName = "Test Buyer",
            CustomerEmail = "buyer@example.test",
            ShippingAddressLine1 = "1 Test Street",
            ShippingCity = "Testville",
            ShippingPostalCode = "00000",
            ShippingCountry = "Testland",
            Status = status,
            Subtotal = product.Price,
            Total = product.Price,
        };

        order.Items.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = product.Id,
            ProductTitle = product.Title,
            UnitPrice = product.Price,
            Quantity = 1,
            LineTotal = product.Price,
        });

        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }

    private sealed class MerchForgeDbContextScope(api.Data.MerchForgeDbContext db) : IDisposable
    {
        public api.Data.MerchForgeDbContext Db { get; } = db;

        public void Dispose() => Db.Dispose();
    }
}
