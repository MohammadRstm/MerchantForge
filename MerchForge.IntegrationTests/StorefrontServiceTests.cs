using FluentAssertions;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.Storefront;
using MerchForge.IntegrationTests.Fakes;

namespace MerchForge.IntegrationTests;

/// <summary>
/// The public storefront API, exercised through the real service + repository +
/// provider stack against a real database.
/// </summary>
public class StorefrontServiceTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _fashionBusiness = null!;
    private Business _otherFashionBusiness = null!;
    private Business _restaurantBusiness = null!;
    private Business _noDomainBusiness = null!;
    private Product _sneakers = null!;
    private Product _boots = null!;
    private Product _tee = null!;
    private Product _otherBusinessShoe = null!;

    public StorefrontServiceTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _fashionBusiness = await _fixture.CreateBusinessAsync(
            "Isolation Fashion A", CatalogDatabaseFixture.FashionDomainId, "EUR");

        _otherFashionBusiness = await _fixture.CreateBusinessAsync(
            "Isolation Fashion B", CatalogDatabaseFixture.FashionDomainId);

        _restaurantBusiness = await _fixture.CreateBusinessAsync(
            "Isolation Restaurant", CatalogDatabaseFixture.RestaurantDomainId);

        _noDomainBusiness = await _fixture.CreateBusinessAsync(
            "Isolation No Domain", domainId: null);

        _sneakers = await _fixture.CreateProductAsync(
            _fashionBusiness.Id, CatalogDatabaseFixture.ShoesCategoryId,
            "Urban Sneakers", 120m,
            """{"colors":["Black"],"waterproof":false}""",
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        _boots = await _fixture.CreateProductAsync(
            _fashionBusiness.Id, CatalogDatabaseFixture.ShoesCategoryId,
            "Leather Boots", 210.50m, null,
            new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc));

        _tee = await _fixture.CreateProductAsync(
            _fashionBusiness.Id, CatalogDatabaseFixture.ShirtsCategoryId,
            "Basic Tee", 22m, null,
            new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));

        // Same domain, same category, different business — the isolation trap.
        _otherBusinessShoe = await _fixture.CreateProductAsync(
            _otherFashionBusiness.Id, CatalogDatabaseFixture.ShoesCategoryId,
            "Rival Sneakers", 99m, null,
            new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Seeds a product with a real, multi-image gallery inserted deliberately out of
    /// DisplayOrder, to prove reads sort by DisplayOrder rather than insertion order.
    /// Local to each test that needs it rather than shared setup, since several
    /// existing tests assert exact product counts for _fashionBusiness.
    /// </summary>
    private async Task<Product> SeedGalleryProductAsync()
    {
        await using var seed = _fixture.CreateContext();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = _fashionBusiness.Id,
            CategoryId = CatalogDatabaseFixture.ShirtsCategoryId,
            Title = "Gallery Shirt",
            Description = "A shirt with a gallery.",
            Price = 45m,
            ImageUrl = "/img/first.png",
            CreatedAt = new DateTime(2026, 2, 6, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 2, 6, 0, 0, 0, DateTimeKind.Utc),
        };
        seed.Products.Add(product);

        seed.ProductImages.AddRange(
            new ProductImage { Id = Guid.NewGuid(), ProductId = product.Id, Url = "/img/third.png", IsMain = false, DisplayOrder = 2 },
            new ProductImage { Id = Guid.NewGuid(), ProductId = product.Id, Url = "/img/first.png", IsMain = true, DisplayOrder = 0, Width = 800, Height = 600 },
            new ProductImage { Id = Guid.NewGuid(), ProductId = product.Id, Url = "/img/second.png", IsMain = false, DisplayOrder = 1 });

        await seed.SaveChangesAsync();

        return product;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private StorefrontService CreateService(out MerchForgeDbContextScope scope)
    {
        scope = new MerchForgeDbContextScope(_fixture.CreateContext());
        return new StorefrontService(new StorefrontRepository(scope.Db, TestImageUrls.Resolver), new OrderRepository(scope.Db, TestImageUrls.Resolver));
    }

    private sealed class MerchForgeDbContextScope(api.Data.MerchForgeDbContext db) : IDisposable
    {
        public api.Data.MerchForgeDbContext Db { get; } = db;
        public void Dispose() => Db.Dispose();
    }

    // ---------- business ----------

    [Fact]
    public async Task GetBusiness_returns_store_configuration_and_domain()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var business = await service.GetBusinessAsync(_fashionBusiness.Id);

        business.Name.Should().Be("Isolation Fashion A");
        business.Currency.Should().Be("EUR");
        business.Locale.Should().Be("en-US");
        business.Domain.Should().NotBeNull();
        business.Domain!.Slug.Should().Be("fashion");
    }

    [Fact]
    public async Task GetBusiness_returns_null_domain_when_none_selected()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        (await service.GetBusinessAsync(_noDomainBusiness.Id)).Domain.Should().BeNull();
    }

    [Fact]
    public async Task GetBusiness_throws_for_an_unknown_business()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var act = async () => await service.GetBusinessAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessNotFoundException>();
    }

    // ---------- categories ----------

    [Fact]
    public async Task GetCategories_returns_only_this_businesss_domain_categories()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var fashion = await service.GetCategoriesAsync(_fashionBusiness.Id);
        var restaurant = await service.GetCategoriesAsync(_restaurantBusiness.Id);

        fashion.Select(c => c.Slug).Should().BeEquivalentTo(["shoes", "shirts", "accessories"]);
        restaurant.Select(c => c.Slug).Should().BeEquivalentTo(["pizza", "burgers", "drinks"]);

        // A Fashion storefront must never be offered a Restaurant category.
        fashion.Select(c => c.Id).Should().NotIntersectWith(restaurant.Select(c => c.Id));
    }

    [Fact]
    public async Task GetCategories_counts_products_per_business_not_platform_wide()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var mine = await service.GetCategoriesAsync(_fashionBusiness.Id);
        var theirs = await service.GetCategoriesAsync(_otherFashionBusiness.Id);

        // Both businesses share the Shoes category, but must see only their own counts.
        mine.Single(c => c.Slug == "shoes").ProductCount.Should().Be(2);
        theirs.Single(c => c.Slug == "shoes").ProductCount.Should().Be(1);
        mine.Single(c => c.Slug == "accessories").ProductCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCategories_includes_this_businesss_own_custom_categories()
    {
        await using (var seed = _fixture.CreateContext())
        {
            seed.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
                BusinessId = _fashionBusiness.Id,
                Name = "Upcycled",
                Slug = "upcycled",
                DisplayOrder = 100,
            });
            await seed.SaveChangesAsync();
        }

        var service = CreateService(out var scope);
        using var _ = scope;

        var categories = await service.GetCategoriesAsync(_fashionBusiness.Id);

        categories.Should().Contain(c => c.Slug == "upcycled");
    }

    [Fact]
    public async Task GetCategories_never_leaks_another_businesss_custom_category()
    {
        // Both businesses are in the Fashion domain, so a domain-only scoping bug
        // would show B's private category on A's storefront.
        await using (var seed = _fixture.CreateContext())
        {
            seed.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
                BusinessId = _otherFashionBusiness.Id,
                Name = "Rival Secret Line",
                Slug = "rival-secret-line",
                DisplayOrder = 100,
            });
            await seed.SaveChangesAsync();
        }

        var service = CreateService(out var scope);
        using var _ = scope;

        var mine = await service.GetCategoriesAsync(_fashionBusiness.Id);
        var theirs = await service.GetCategoriesAsync(_otherFashionBusiness.Id);

        mine.Should().NotContain(c => c.Slug == "rival-secret-line");
        theirs.Should().Contain(c => c.Slug == "rival-secret-line");
    }

    [Fact]
    public async Task GetCategories_is_empty_for_a_business_with_no_domain()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        (await service.GetCategoriesAsync(_noDomainBusiness.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategories_throws_for_an_unknown_business()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        // Distinguishable from "no categories yet", which is an empty list.
        var act = async () => await service.GetCategoriesAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessNotFoundException>();
    }

    // ---------- product listing ----------

    [Fact]
    public async Task GetProducts_returns_only_the_requested_businesss_products()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var page = await service.GetProductsAsync(_fashionBusiness.Id, new StorefrontProductsQueryRequest());

        page.TotalCount.Should().Be(3);
        page.Items.Select(p => p.Title)
            .Should().NotContain("Rival Sneakers", "that product belongs to another business");
    }

    [Fact]
    public async Task GetProducts_filters_by_category()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var page = await service.GetProductsAsync(
            _fashionBusiness.Id,
            new StorefrontProductsQueryRequest { CategoryId = CatalogDatabaseFixture.ShoesCategoryId });

        page.TotalCount.Should().Be(2);
        page.Items.Should().OnlyContain(p => p.Category.Slug == "shoes");
    }

    [Fact]
    public async Task GetProducts_category_filter_cannot_widen_past_the_business()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        // Shoes contains a product from another business; filtering must still not reveal it.
        var page = await service.GetProductsAsync(
            _otherFashionBusiness.Id,
            new StorefrontProductsQueryRequest { CategoryId = CatalogDatabaseFixture.ShoesCategoryId });

        page.Items.Should().ContainSingle().Which.Title.Should().Be("Rival Sneakers");
    }

    [Fact]
    public async Task GetProducts_filtering_by_another_domains_category_returns_nothing()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var page = await service.GetProductsAsync(
            _fashionBusiness.Id,
            new StorefrontProductsQueryRequest { CategoryId = CatalogDatabaseFixture.PizzaCategoryId });

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetProducts_searches_titles()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var page = await service.GetProductsAsync(
            _fashionBusiness.Id,
            new StorefrontProductsQueryRequest { Search = "sneak" });

        page.Items.Should().ContainSingle().Which.Title.Should().Be("Urban Sneakers");
    }

    [Fact]
    public async Task GetProducts_filters_by_price_range()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var page = await service.GetProductsAsync(
            _fashionBusiness.Id,
            new StorefrontProductsQueryRequest { MinPrice = 50m, MaxPrice = 150m });

        page.Items.Should().ContainSingle().Which.Title.Should().Be("Urban Sneakers");
    }

    [Fact]
    public async Task GetProducts_sorts_by_price()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var page = await service.GetProductsAsync(
            _fashionBusiness.Id,
            new StorefrontProductsQueryRequest
            {
                SortBy = ProductSortField.Price,
                SortDescending = false,
            });

        page.Items.Select(p => p.Price).Should().BeInAscendingOrder();
        page.Items.First().Title.Should().Be("Basic Tee");
    }

    [Fact]
    public async Task GetProducts_paginates_without_repeating_or_skipping_rows()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        Task<api.DTOs.Common.PagedResult<StorefrontProductResponse>> Page(int page) =>
            service.GetProductsAsync(
                _fashionBusiness.Id,
                new StorefrontProductsQueryRequest
                {
                    Page = page,
                    PageSize = 2,
                    SortBy = ProductSortField.Price,
                    SortDescending = false,
                });

        var first = await Page(1);
        var second = await Page(2);

        first.Items.Should().HaveCount(2);
        second.Items.Should().HaveCount(1);
        first.TotalCount.Should().Be(3);
        first.TotalPages.Should().Be(2);

        first.Items.Select(p => p.Id).Should().NotIntersectWith(second.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProducts_includes_metadata_in_listings()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var page = await service.GetProductsAsync(_fashionBusiness.Id, new StorefrontProductsQueryRequest());

        var sneakers = page.Items.Single(p => p.Id == _sneakers.Id);

        sneakers.Metadata.Should().NotBeNull();
        sneakers.Metadata!.RootElement.GetProperty("waterproof")
            .ValueKind.Should().Be(System.Text.Json.JsonValueKind.False);

        page.Items.Single(p => p.Id == _boots.Id).Metadata.Should().BeNull();
    }

    [Fact]
    public async Task GetProducts_includes_each_products_images_sorted_by_display_order()
    {
        var galleryProduct = await SeedGalleryProductAsync();

        var service = CreateService(out var scope);
        using var _ = scope;

        var page = await service.GetProductsAsync(_fashionBusiness.Id, new StorefrontProductsQueryRequest());

        var listed = page.Items.Single(p => p.Id == galleryProduct.Id);
        listed.Images.Select(i => i.Url).Should().Equal("/img/first.png", "/img/second.png", "/img/third.png");

        // Products seeded without a gallery still return an empty array, not null.
        page.Items.Single(p => p.Id == _sneakers.Id).Images.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProducts_throws_for_an_unknown_business()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var act = async () => await service.GetProductsAsync(Guid.NewGuid(), new StorefrontProductsQueryRequest());

        await act.Should().ThrowAsync<BusinessNotFoundException>();
    }

    // ---------- product detail ----------

    [Fact]
    public async Task GetProduct_returns_the_full_detail_shape()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var product = await service.GetProductAsync(_fashionBusiness.Id, _sneakers.Id);

        product.Title.Should().Be("Urban Sneakers");
        product.Description.Should().NotBeNullOrWhiteSpace();
        product.Category.Slug.Should().Be("shoes");
        product.Metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProduct_returns_all_images_sorted_by_display_order()
    {
        var galleryProduct = await SeedGalleryProductAsync();

        var service = CreateService(out var scope);
        using var _ = scope;

        var detail = await service.GetProductAsync(_fashionBusiness.Id, galleryProduct.Id);

        detail.ImageUrl.Should().Be("/img/first.png");
        detail.Images.Should().HaveCount(3);
        // Equal, not BeEquivalentTo: order matters here, not just membership.
        detail.Images.Select(i => i.Url).Should().Equal("/img/first.png", "/img/second.png", "/img/third.png");
        detail.Images.Select(i => i.DisplayOrder).Should().Equal(0, 1, 2);
        detail.Images.Should().ContainSingle(i => i.IsMain).Which.Url.Should().Be("/img/first.png");

        var mainImage = detail.Images.Single(i => i.IsMain);
        mainImage.Width.Should().Be(800);
        mainImage.Height.Should().Be(600);
    }

    [Fact]
    public async Task GetProduct_throws_for_a_product_belonging_to_another_business()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        // The product exists — just not for this business.
        var act = async () => await service.GetProductAsync(_fashionBusiness.Id, _otherBusinessShoe.Id);

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task GetProduct_reports_a_foreign_product_identically_to_a_nonexistent_one()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var foreign = await Record.ExceptionAsync(
            () => service.GetProductAsync(_fashionBusiness.Id, _otherBusinessShoe.Id));

        var missing = await Record.ExceptionAsync(
            () => service.GetProductAsync(_fashionBusiness.Id, Guid.NewGuid()));

        // Identical errors, so a storefront cannot probe whether an id exists elsewhere.
        foreign.Should().BeOfType<ProductNotFoundException>();
        missing.Should().BeOfType<ProductNotFoundException>();
        foreign!.Message.Should().Be(missing!.Message);
    }

    // ---------- related products ----------

    [Fact]
    public async Task GetRelatedProducts_returns_same_category_siblings_excluding_itself()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var related = await service.GetRelatedProductsAsync(_fashionBusiness.Id, _sneakers.Id, 4);

        related.Should().ContainSingle().Which.Id.Should().Be(_boots.Id);
        related.Should().NotContain(p => p.Id == _sneakers.Id);
    }

    [Fact]
    public async Task GetRelatedProducts_never_crosses_business_boundaries()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var related = await service.GetRelatedProductsAsync(_fashionBusiness.Id, _sneakers.Id, 10);

        // Rival Sneakers is in the same category, but a different business.
        related.Should().NotContain(p => p.Id == _otherBusinessShoe.Id);
    }

    [Fact]
    public async Task GetRelatedProducts_includes_images_sorted_by_display_order()
    {
        // Same category as _tee (Shirts), so it comes back as a related sibling.
        var galleryProduct = await SeedGalleryProductAsync();

        var service = CreateService(out var scope);
        using var _ = scope;

        var related = await service.GetRelatedProductsAsync(_fashionBusiness.Id, _tee.Id, 4);

        var listed = related.Should().ContainSingle().Which;
        listed.Id.Should().Be(galleryProduct.Id);
        listed.Images.Select(i => i.Url).Should().Equal("/img/first.png", "/img/second.png", "/img/third.png");
    }

    [Fact]
    public async Task GetRelatedProducts_returns_empty_when_the_product_has_no_siblings()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        (await service.GetRelatedProductsAsync(_fashionBusiness.Id, _tee.Id, 4))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task GetRelatedProducts_throws_for_a_foreign_product()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        var act = async () => await service.GetRelatedProductsAsync(
            _fashionBusiness.Id, _otherBusinessShoe.Id, 4);

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task GetRelatedProducts_clamps_the_limit()
    {
        var service = CreateService(out var scope);
        using var _ = scope;

        // Must not become an unbounded catalog dump, and must not fail on 0/negative.
        var act = async () => await service.GetRelatedProductsAsync(
            _fashionBusiness.Id, _sneakers.Id, int.MaxValue);

        await act.Should().NotThrowAsync();

        (await service.GetRelatedProductsAsync(_fashionBusiness.Id, _sneakers.Id, 0))
            .Should().NotBeNull();
    }
}
