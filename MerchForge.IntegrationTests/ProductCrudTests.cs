using System.Text.Json;
using FluentAssertions;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.Subscription;
using MerchForge.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// Product create/update/delete from the merchant dashboard, including the two
/// invariants the database cannot express: a product's category must be usable by
/// its business, and its metadata must match that business's opted-in shape.
/// </summary>
public class ProductCrudTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _business = null!;
    private Business _rivalBusiness = null!;

    public ProductCrudTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _business = await _fixture.CreateBusinessAsync("CRUD Fashion Co", CatalogDatabaseFixture.FashionDomainId);
        _rivalBusiness = await _fixture.CreateBusinessAsync("CRUD Rival Co", CatalogDatabaseFixture.FashionDomainId);

        // Opt the business into a field of each value type.
        await using var db = _fixture.CreateContext();

        var tracked = await db.Businesses.FirstAsync(b => b.Id == _business.Id);

        tracked.MetadataShape = JsonDocument.Parse("""
            {"fields":[
              {"key":"colors","label":"Colors","valueType":"TextList"},
              {"key":"material","label":"Material","valueType":"Text"},
              {"key":"handmade","label":"Handmade","valueType":"Boolean"}
            ]}
            """);

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private BusinessDashboardService CreateService(out api.Data.MerchForgeDbContext db) =>
        CreateService(out db, out _);

    private BusinessDashboardService CreateService(
        out api.Data.MerchForgeDbContext db,
        out FakeProductImageService images)
    {
        db = _fixture.CreateContext();
        images = new FakeProductImageService();

        var featureCreditRepo = new FeatureCreditRepository(db);
        var subscriptionRepository = new SubscriptionRepository(db);
        var subscriptionService = new SubscriptionService(subscriptionRepository, featureCreditRepo);
        var featureCreditService = new FeatureCreditService(featureCreditRepo, subscriptionService, subscriptionRepository);

        return new BusinessDashboardService(
            new BusinessDashboardRepository(db, TestImageUrls.Resolver),
            subscriptionRepository,
            new WebsiteTemplateRequestRepository(db),
            new OrderRepository(db, TestImageUrls.Resolver),
            new ProductReviewRepository(db),
            new FakeBackgroundJobClient(),
            featureCreditService,
            TestImageUrls.Resolver,
            images);
    }

    /// <summary>
    /// Image references are validated against the caller's business now, so fixtures
    /// have to carry a real object key rather than any old path - which is also why
    /// this is no longer static.
    /// </summary>
    private SaveProductRequest Request(
        Guid categoryId,
        string title = "Test Product",
        decimal price = 10m,
        string? imageUrl = null,
        Dictionary<string, JsonElement>? metadata = null) => new()
        {
            Title = title,
            Description = "A description.",
            Price = price,
            CategoryId = categoryId,
            // At least one image, exactly one main, is a hard requirement now — the
            // validator enforces it at the controller layer, but these tests call the
            // service directly, so the request itself has to already satisfy it.
            Images = [new ProductImageRequest
            {
                Url = imageUrl ?? TestImageUrls.ImageKey(_business.Id, "default"),
                IsMain = true,
            }],
            Metadata = metadata,
        };

    private static Dictionary<string, JsonElement> Json(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    // ---- create ----

    [Fact]
    public async Task Creates_a_product_with_the_fixed_fields()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId, "Runner", 49.99m,
                TestImageUrls.ImageKey(_business.Id, "y")));

        created.Title.Should().Be("Runner");
        created.Price.Should().Be(49.99m);
        created.CategoryId.Should().Be(CatalogDatabaseFixture.ShoesCategoryId);
        created.CategoryName.Should().Be("Shoes");
        created.ImageUrl.Should().Be(
            TestImageUrls.PublicImageUrl(_business.Id, "y"),
            "the key is stored, and resolved to a url on the way back out");
        created.Metadata.Should().BeNull("no optional fields were supplied");
    }

    [Fact]
    public async Task Creates_a_product_with_metadata_of_each_value_type()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateProductAsync(
            _business.Id,
            Request(
                CatalogDatabaseFixture.ShoesCategoryId,
                metadata: Json("""{"colors":["Black","White"],"material":"Suede","handmade":true}""")));

        var root = created.Metadata!.RootElement;

        root.GetProperty("colors").EnumerateArray().Select(e => e.GetString())
            .Should().Equal(["Black", "White"]);
        root.GetProperty("material").GetString().Should().Be("Suede");
        root.GetProperty("handmade").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Metadata_fields_are_all_optional()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        // Only one of the three opted-in fields supplied.
        var created = await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId, metadata: Json("""{"material":"Cotton"}""")));

        created.Metadata!.RootElement.TryGetProperty("colors", out var colors).Should().BeFalse();
        colors.ValueKind.Should().Be(JsonValueKind.Undefined);
        created.Metadata.RootElement.GetProperty("material").GetString().Should().Be("Cotton");
    }

    [Fact]
    public async Task Blank_text_and_empty_lists_are_dropped_rather_than_stored()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId, metadata: Json("""{"material":"   ","colors":[]}""")));

        created.Metadata.Should().BeNull("a blank field is unanswered, not a value");
    }

    [Fact]
    public async Task False_booleans_are_kept_because_no_is_real_information()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId, metadata: Json("""{"handmade":false}""")));

        created.Metadata!.RootElement.GetProperty("handmade").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Rejects_a_metadata_key_the_business_did_not_opt_into()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        // "sizes" is a real Fashion field, but this business didn't enable it.
        var act = async () => await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId, metadata: Json("""{"sizes":["M"]}""")));

        await act.Should().ThrowAsync<InvalidProductMetadataException>();
    }

    [Theory]
    [InlineData("""{"colors":"Black"}""")]     // TextList given a string
    [InlineData("""{"material":123}""")]       // Text given a number
    [InlineData("""{"handmade":"yes"}""")]     // Boolean given a string
    [InlineData("""{"colors":[1,2]}""")]       // TextList given numbers
    public async Task Rejects_metadata_whose_value_type_does_not_match_the_definition(string metadataJson)
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var act = async () => await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId, metadata: Json(metadataJson)));

        await act.Should().ThrowAsync<InvalidProductMetadataException>();
    }

    [Fact]
    public async Task Rejects_a_category_from_another_domain()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        // This is the invariant MariaDB can't express as a CHECK, so it has to hold
        // here: a Fashion business must not be able to use a Restaurant category.
        var act = async () => await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.PizzaCategoryId));

        await act.Should().ThrowAsync<InvalidProductCategoryException>();
    }

    [Fact]
    public async Task Rejects_another_businesss_private_custom_category()
    {
        Guid rivalCategoryId;

        await using (var seed = _fixture.CreateContext())
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
                BusinessId = _rivalBusiness.Id,
                Name = "Rival Only",
                Slug = "rival-only",
                DisplayOrder = 100,
            };
            seed.Categories.Add(category);
            await seed.SaveChangesAsync();
            rivalCategoryId = category.Id;
        }

        var service = CreateService(out var db);
        await using var _ = db;

        var act = async () => await service.CreateProductAsync(_business.Id, Request(rivalCategoryId));

        await act.Should().ThrowAsync<InvalidProductCategoryException>();
    }

    [Fact]
    public async Task Accepts_the_businesss_own_custom_category()
    {
        Guid ownCategoryId;

        await using (var seed = _fixture.CreateContext())
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
                BusinessId = _business.Id,
                Name = "Upcycled",
                Slug = "upcycled-crud",
                DisplayOrder = 100,
            };
            seed.Categories.Add(category);
            await seed.SaveChangesAsync();
            ownCategoryId = category.Id;
        }

        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateProductAsync(_business.Id, Request(ownCategoryId));

        created.CategoryName.Should().Be("Upcycled");
    }

    // ---- read / update / delete ----

    [Fact]
    public async Task Product_form_exposes_usable_categories_and_opted_in_fields()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var form = await service.GetProductFormAsync(_business.Id);

        form.Categories.Select(c => c.Name).Should().Contain(["Shoes", "Shirts", "Accessories"]);
        form.MetadataFields.Select(f => f.Key).Should().Equal(["colors", "material", "handmade"]);
        form.MetadataFields.Single(f => f.Key == "colors").ValueType.Should().Be("TextList");
    }

    [Fact]
    public async Task Updates_a_product_and_can_clear_its_metadata()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId, metadata: Json("""{"material":"Suede"}""")));

        var updated = await service.UpdateProductAsync(
            _business.Id,
            created.Id,
            Request(CatalogDatabaseFixture.ShirtsCategoryId, "Renamed", 99m));

        updated.Title.Should().Be("Renamed");
        updated.Price.Should().Be(99m);
        updated.CategoryName.Should().Be("Shirts");
        updated.Metadata.Should().BeNull("submitting no metadata clears it");
        updated.UpdatedAt.Should().BeOnOrAfter(created.CreatedAt);
    }

    [Fact]
    public async Task Creates_a_product_with_a_multi_image_gallery_and_syncs_the_main_image()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var request = new SaveProductRequest
        {
            Title = "Gallery Product",
            Description = "A description.",
            Price = 20m,
            CategoryId = CatalogDatabaseFixture.ShoesCategoryId,
            Images =
            [
                new ProductImageRequest { Url = TestImageUrls.ImageKey(_business.Id, "main"), IsMain = true, Width = 800, Height = 600 },
                new ProductImageRequest { Url = TestImageUrls.ImageKey(_business.Id, "alt"), IsMain = false },
            ],
        };

        var created = await service.CreateProductAsync(_business.Id, request);

        created.ImageUrl.Should().Be(
            TestImageUrls.PublicImageUrl(_business.Id, "main"), "ImageUrl tracks whichever image is IsMain");
        created.Images.Should().HaveCount(2);
        created.Images.Should().ContainSingle(i => i.IsMain).Which.Url
            .Should().Be(TestImageUrls.PublicImageUrl(_business.Id, "main"));
        created.Images.First(i => i.IsMain).Width.Should().Be(800);
        created.Images.Should().ContainSingle(i => !i.IsMain).Which.Url
            .Should().Be(TestImageUrls.PublicImageUrl(_business.Id, "alt"));
    }

    [Fact]
    public async Task Updating_a_product_fully_replaces_its_image_gallery()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId,
                imageUrl: TestImageUrls.ImageKey(_business.Id, "original")));

        var updated = await service.UpdateProductAsync(
            _business.Id,
            created.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId,
                imageUrl: TestImageUrls.ImageKey(_business.Id, "replacement")));

        updated.ImageUrl.Should().Be(TestImageUrls.PublicImageUrl(_business.Id, "replacement"));
        updated.Images.Should().ContainSingle().Which.Url
            .Should().Be(TestImageUrls.PublicImageUrl(_business.Id, "replacement"));
    }

    [Fact]
    public async Task Deletes_a_product()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateProductAsync(_business.Id, Request(CatalogDatabaseFixture.ShoesCategoryId));

        await service.DeleteProductAsync(_business.Id, created.Id);

        var act = async () => await service.GetProductAsync(_business.Id, created.Id);
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    /// <summary>
    /// Storage cleanup happens after the rows are gone, and only for a product with no
    /// orders - which DeleteProductAsync already refuses. That ordering is what keeps a
    /// failed commit from stranding a live row against a deleted image.
    /// </summary>
    [Fact]
    public async Task Deleting_a_product_cleans_up_its_stored_images()
    {
        var service = CreateService(out var db, out var images);
        await using var _ = db;

        var created = await service.CreateProductAsync(
            _business.Id,
            Request(CatalogDatabaseFixture.ShoesCategoryId,
                imageUrl: TestImageUrls.ImageKey(_business.Id, "doomed")));

        await service.DeleteProductAsync(_business.Id, created.Id);

        images.DeletedValues.Should().Contain(TestImageUrls.ImageKey(_business.Id, "doomed"));
    }

    [Fact]
    public async Task A_product_that_cannot_be_deleted_keeps_its_images()
    {
        var service = CreateService(out var db, out var images);
        await using var _ = db;

        var mine = await service.CreateProductAsync(_business.Id, Request(CatalogDatabaseFixture.ShoesCategoryId));

        var delete = async () => await service.DeleteProductAsync(_rivalBusiness.Id, mine.Id);
        await delete.Should().ThrowAsync<ProductNotFoundException>();

        images.DeletedValues.Should().BeEmpty("a refused delete must not take the images with it");
    }

    [Fact]
    public async Task One_business_cannot_read_update_or_delete_anothers_product()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var mine = await service.CreateProductAsync(_business.Id, Request(CatalogDatabaseFixture.ShoesCategoryId));

        var read = async () => await service.GetProductAsync(_rivalBusiness.Id, mine.Id);
        var update = async () => await service.UpdateProductAsync(
            _rivalBusiness.Id, mine.Id, Request(CatalogDatabaseFixture.ShoesCategoryId, "Hijacked"));
        var delete = async () => await service.DeleteProductAsync(_rivalBusiness.Id, mine.Id);

        await read.Should().ThrowAsync<ProductNotFoundException>();
        await update.Should().ThrowAsync<ProductNotFoundException>();
        await delete.Should().ThrowAsync<ProductNotFoundException>();

        // And the product is untouched.
        (await service.GetProductAsync(_business.Id, mine.Id)).Title.Should().Be("Test Product");
    }

    [Fact]
    public async Task Deleting_a_product_leaves_its_category_intact()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateProductAsync(_business.Id, Request(CatalogDatabaseFixture.ShoesCategoryId));
        await service.DeleteProductAsync(_business.Id, created.Id);

        await using var verify = _fixture.CreateContext();
        (await verify.Categories.AnyAsync(c => c.Id == CatalogDatabaseFixture.ShoesCategoryId))
            .Should().BeTrue();
    }
}
