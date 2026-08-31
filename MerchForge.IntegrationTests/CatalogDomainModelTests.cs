using System.Text.Json;
using FluentAssertions;
using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace MerchForge.IntegrationTests;

/// <summary>
/// The database-level guarantees the catalog rests on: relationships, seeding,
/// delete behaviour, and JSON metadata storage.
/// </summary>
public class CatalogDomainModelTests : IClassFixture<CatalogDatabaseFixture>
{
    private readonly CatalogDatabaseFixture _fixture;

    public CatalogDomainModelTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migration_seeds_domains_and_their_categories()
    {
        await using var db = _fixture.CreateContext();

        var domains = await db.BusinessDomains
            .Include(d => d.Categories)
            .OrderBy(d => d.Name)
            .ToListAsync();

        domains.Select(d => d.Slug)
            .Should().BeEquivalentTo(["electronics", "fashion", "grocery", "restaurant"]);

        domains.Should().OnlyContain(d => d.Categories.Count > 0);
    }

    [Fact]
    public async Task Category_slugs_are_unique_per_domain_but_may_repeat_across_domains()
    {
        await using var db = _fixture.CreateContext();

        // "accessories" legitimately exists under both Fashion and Electronics, as
        // two distinct rows. A globally-unique slug would have made that impossible.
        var accessories = await db.Categories
            .Where(c => c.Slug == "accessories")
            .ToListAsync();

        accessories.Should().HaveCount(2);
        accessories.Select(c => c.BusinessDomainId).Should().OnlyHaveUniqueItems();
        accessories.Select(c => c.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Duplicate_slug_within_the_same_businesss_custom_categories_is_rejected()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Duplicate Slug Co", CatalogDatabaseFixture.FashionDomainId);

        await using var db = _fixture.CreateContext();

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
            BusinessId = business.Id,
            Name = "Vintage",
            Slug = "vintage",
            DisplayOrder = 100,
        });
        await db.SaveChangesAsync();

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
            BusinessId = business.Id,
            Name = "Vintage Again",
            Slug = "vintage", // same business, same domain, same slug
            DisplayOrder = 101,
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Two_different_businesses_can_each_have_a_custom_category_with_the_same_slug()
    {
        var businessA = await _fixture.CreateBusinessAsync(
            "Vintage Shop A", CatalogDatabaseFixture.FashionDomainId);
        var businessB = await _fixture.CreateBusinessAsync(
            "Vintage Shop B", CatalogDatabaseFixture.FashionDomainId);

        await using var db = _fixture.CreateContext();

        db.Categories.AddRange(
            new Category
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
                BusinessId = businessA.Id,
                Name = "Vintage",
                Slug = "vintage",
                DisplayOrder = 100,
            },
            new Category
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
                BusinessId = businessB.Id,
                Name = "Vintage",
                Slug = "vintage",
                DisplayOrder = 100,
            });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync(
            "each business's custom categories are scoped by BusinessId, so identical slugs for different businesses don't collide");
    }

    [Fact]
    public async Task Database_does_not_prevent_two_platform_categories_sharing_a_slug()
    {
        // Documents a known, accepted gap: MariaDB treats every NULL as distinct in a
        // unique index, so (BusinessDomainId, BusinessId, Slug) does not stop two
        // platform (BusinessId IS NULL) rows from sharing a slug. Accepted because
        // platform categories are only ever created by seeding, which is under this
        // codebase's own control — see CategoryConfiguration's index comment. Custom
        // categories created through registration are protected at the application
        // layer instead (DomainService.BuildCustomCategoriesAsync checks existing
        // platform slugs before creating anything).
        await using var db = _fixture.CreateContext();

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
            BusinessId = null,
            Name = "Shoes Duplicate",
            Slug = "shoes", // already taken by the seeded platform "Shoes" category
            DisplayOrder = 99,
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Business_can_be_associated_with_a_domain()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Domain Assoc Co",
            CatalogDatabaseFixture.FashionDomainId);

        await using var db = _fixture.CreateContext();

        var loaded = await db.Businesses
            .Include(b => b.BusinessDomain)
            .FirstAsync(b => b.Id == business.Id);

        loaded.BusinessDomain.Should().NotBeNull();
        loaded.BusinessDomain!.Slug.Should().Be("fashion");
    }

    [Fact]
    public async Task Business_without_a_domain_is_valid()
    {
        // Businesses created before domains existed genuinely have none, and
        // onboarding does not ask yet. Requiring it would mean inventing data.
        var business = await _fixture.CreateBusinessAsync("No Domain Co", domainId: null);

        await using var db = _fixture.CreateContext();

        var loaded = await db.Businesses.FirstAsync(b => b.Id == business.Id);

        loaded.BusinessDomainId.Should().BeNull();
    }

    [Fact]
    public async Task Product_requires_a_category_that_exists()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Bad Category Co",
            CatalogDatabaseFixture.FashionDomainId);

        await using var db = _fixture.CreateContext();

        db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            CategoryId = Guid.NewGuid(), // no such category
            Title = "Orphan",
            Description = "Should not persist.",
            Price = 10m,
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Deleting_a_category_is_restricted_while_products_reference_it()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Restrict Co",
            CatalogDatabaseFixture.FashionDomainId);

        await _fixture.CreateProductAsync(
            business.Id, CatalogDatabaseFixture.ShoesCategoryId, "Boots", 100m);

        await using var db = _fixture.CreateContext();

        var category = await db.Categories.FirstAsync(c => c.Id == CatalogDatabaseFixture.ShoesCategoryId);
        db.Categories.Remove(category);

        // The whole point of RESTRICT here: deleting shared reference data must never
        // silently delete every business's products in that category.
        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Deleting_a_business_cascades_to_its_products_only()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Cascade Co",
            CatalogDatabaseFixture.FashionDomainId);

        await _fixture.CreateProductAsync(
            business.Id, CatalogDatabaseFixture.ShirtsCategoryId, "Doomed Tee", 20m);

        await using (var db = _fixture.CreateContext())
        {
            var loaded = await db.Businesses.FirstAsync(b => b.Id == business.Id);
            db.Businesses.Remove(loaded);
            await db.SaveChangesAsync();
        }

        await using var verify = _fixture.CreateContext();

        (await verify.Products.CountAsync(p => p.BusinessId == business.Id))
            .Should().Be(0, "a business owns its catalog");

        (await verify.Categories.AnyAsync(c => c.Id == CatalogDatabaseFixture.ShirtsCategoryId))
            .Should().BeTrue("categories are shared platform data, not owned by the business");

        (await verify.BusinessDomains.AnyAsync(d => d.Id == CatalogDatabaseFixture.FashionDomainId))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Metadata_round_trips_through_the_json_column_with_value_types_intact()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Metadata Co",
            CatalogDatabaseFixture.FashionDomainId);

        var product = await _fixture.CreateProductAsync(
            business.Id,
            CatalogDatabaseFixture.ShoesCategoryId,
            "Metadata Sneakers",
            120m,
            """{"colors":["Black","White"],"sizes":["40","41"],"material":"Canvas","waterproof":true,"stock":7}""");

        await using var db = _fixture.CreateContext();

        var loaded = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);

        loaded.Metadata.Should().NotBeNull();

        var root = loaded.Metadata!.RootElement;

        // The point is that these are real JSON types, not everything-is-a-string.
        root.GetProperty("colors").ValueKind.Should().Be(JsonValueKind.Array);
        root.GetProperty("colors").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(["Black", "White"]);
        root.GetProperty("material").GetString().Should().Be("Canvas");
        root.GetProperty("waterproof").ValueKind.Should().Be(JsonValueKind.True);
        root.GetProperty("stock").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task Metadata_is_optional()
    {
        var business = await _fixture.CreateBusinessAsync(
            "No Metadata Co",
            CatalogDatabaseFixture.FashionDomainId);

        var product = await _fixture.CreateProductAsync(
            business.Id, CatalogDatabaseFixture.ShirtsCategoryId, "Plain Tee", 15m);

        await using var db = _fixture.CreateContext();

        (await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id))
            .Metadata.Should().BeNull();
    }

    [Fact]
    public async Task Database_rejects_malformed_json_in_the_metadata_column()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Invalid Json Co",
            CatalogDatabaseFixture.FashionDomainId);

        // Bypasses EF to prove the guarantee is the database's, not the app's:
        // MariaDB adds CHECK (json_valid(Metadata)) for a json column.
        await using var conn = new MySqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            "INSERT INTO products (Id, BusinessId, CategoryId, Title, Description, Price, Metadata, CreatedAt, UpdatedAt) " +
            "VALUES (@id, @biz, @cat, 'Broken', 'Broken', 1.00, 'not json at all', NOW(), NOW());",
            conn);

        cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@biz", business.Id.ToString());
        cmd.Parameters.AddWithValue("@cat", CatalogDatabaseFixture.ShoesCategoryId.ToString());

        var act = async () => await cmd.ExecuteNonQueryAsync();

        await act.Should().ThrowAsync<MySqlException>();
    }
}
