using FluentAssertions;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Onboarding;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// The registration-time domain/category flow: what a prospective business owner is
/// offered, and what happens to the custom categories they add.
/// </summary>
public class OnboardingDomainServiceTests : IClassFixture<CatalogDatabaseFixture>
{
    private readonly CatalogDatabaseFixture _fixture;

    public OnboardingDomainServiceTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private DomainService CreateService(out api.Data.MerchForgeDbContext db)
    {
        db = _fixture.CreateContext();
        return new DomainService(new DomainRepository(db));
    }

    [Fact]
    public async Task Domains_lists_the_seeded_verticals()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var domains = await service.GetDomainsAsync();

        domains.Select(d => d.Slug)
            .Should().BeEquivalentTo(["electronics", "fashion", "grocery", "restaurant"]);
    }

    [Fact]
    public async Task Categories_lists_the_domains_platform_categories()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var categories = await service.GetCategoriesAsync(CatalogDatabaseFixture.FashionDomainId);

        categories.Select(c => c.Slug)
            .Should().BeEquivalentTo(["shoes", "shirts", "accessories"]);
    }

    [Fact]
    public async Task Categories_never_suggests_another_businesss_custom_category()
    {
        // The whole point of the custom flag: one business's private category must
        // not show up in the next business owner's registration form.
        var business = await _fixture.CreateBusinessAsync(
            "Private Category Co", CatalogDatabaseFixture.FashionDomainId);

        await using (var seed = _fixture.CreateContext())
        {
            seed.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
                BusinessId = business.Id,
                Name = "Bespoke Tailoring",
                Slug = "bespoke-tailoring",
                DisplayOrder = 100,
            });
            await seed.SaveChangesAsync();
        }

        var service = CreateService(out var db);
        await using var _ = db;

        var categories = await service.GetCategoriesAsync(CatalogDatabaseFixture.FashionDomainId);

        categories.Should().NotContain(c => c.Slug == "bespoke-tailoring");
    }

    [Fact]
    public async Task Categories_throws_for_an_unknown_domain()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var act = async () => await service.GetCategoriesAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessDomainNotFoundException>();
    }

    [Fact]
    public async Task Custom_categories_are_built_scoped_to_the_business_with_generated_slugs()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Slug Gen Co", CatalogDatabaseFixture.FashionDomainId);

        var service = CreateService(out var db);
        await using var _ = db;

        var built = await service.BuildCustomCategoriesAsync(
            business.Id,
            CatalogDatabaseFixture.FashionDomainId,
            ["Vintage Wear", "  Hand-Made Bags!  "]);

        built.Should().HaveCount(2);
        built.Should().OnlyContain(c => c.BusinessId == business.Id);
        built.Should().OnlyContain(c => c.BusinessDomainId == CatalogDatabaseFixture.FashionDomainId);
        built.Select(c => c.Slug).Should().BeEquivalentTo(["vintage-wear", "hand-made-bags"]);
        built.Select(c => c.Name).Should().BeEquivalentTo(["Vintage Wear", "Hand-Made Bags!"]);
    }

    [Fact]
    public async Task Custom_categories_deduplicate_names_that_differ_only_by_case_or_spacing()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Dedupe Co", CatalogDatabaseFixture.FashionDomainId);

        var service = CreateService(out var db);
        await using var _ = db;

        var built = await service.BuildCustomCategoriesAsync(
            business.Id,
            CatalogDatabaseFixture.FashionDomainId,
            ["Vintage", "vintage", "  VINTAGE  ", ""]);

        // All four collapse to one row rather than hitting the unique index later.
        built.Should().ContainSingle().Which.Slug.Should().Be("vintage");
    }

    [Fact]
    public async Task Custom_categories_reject_names_that_already_exist_as_platform_categories()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Reinvent Co", CatalogDatabaseFixture.FashionDomainId);

        var service = CreateService(out var db);
        await using var _ = db;

        // "Shoes" already exists for everyone in Fashion; creating a private
        // duplicate would fragment the catalog for no benefit.
        var act = async () => await service.BuildCustomCategoriesAsync(
            business.Id,
            CatalogDatabaseFixture.FashionDomainId,
            ["shoes"]);

        await act.Should().ThrowAsync<DuplicateCategoryNameException>();
    }

    [Fact]
    public async Task Custom_categories_allow_a_name_that_only_exists_in_a_different_domain()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Cross Domain Co", CatalogDatabaseFixture.FashionDomainId);

        var service = CreateService(out var db);
        await using var _ = db;

        // "Pizza" is a Restaurant category; a Fashion business may legitimately
        // create its own unrelated "Pizza" category.
        var built = await service.BuildCustomCategoriesAsync(
            business.Id,
            CatalogDatabaseFixture.FashionDomainId,
            ["Pizza"]);

        built.Should().ContainSingle().Which.Slug.Should().Be("pizza");
    }

    [Fact]
    public async Task No_custom_category_names_produces_nothing()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Empty Co", CatalogDatabaseFixture.FashionDomainId);

        var service = CreateService(out var db);
        await using var _ = db;

        (await service.BuildCustomCategoriesAsync(
            business.Id, CatalogDatabaseFixture.FashionDomainId, [])).Should().BeEmpty();
    }

    // ---------- product attribute definitions / metadata shape ----------

    [Fact]
    public async Task Product_attributes_are_listed_for_the_domain_in_display_order()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var attributes = await service.GetProductAttributesAsync(CatalogDatabaseFixture.FashionDomainId);

        attributes.Should().NotBeEmpty();
        attributes.Select(a => a.DisplayOrder).Should().BeInAscendingOrder();
        attributes.Should().Contain(a => a.Key == "colors" && a.ValueType == "ColorList");
        attributes.Should().Contain(a => a.Key == "handmade" && a.ValueType == "Boolean");

        // Restaurant-only fields must not leak into Fashion's catalogue.
        attributes.Should().NotContain(a => a.Key == "spicy");
    }

    [Fact]
    public async Task Metadata_shape_snapshots_the_selected_fields()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var shape = await service.BuildMetadataShapeAsync(
            CatalogDatabaseFixture.FashionDomainId,
            ["material", "colors"]);

        shape.Should().NotBeNull();

        var fields = shape!.RootElement.GetProperty("fields");

        // Emitted in the domain's display order (colors=1 before material=3), not the
        // order the caller happened to send them in.
        fields.EnumerateArray().Select(f => f.GetProperty("key").GetString())
            .Should().Equal(["colors", "material"]);

        var colors = fields.EnumerateArray().First();
        colors.GetProperty("label").GetString().Should().Be("Colors");
        colors.GetProperty("valueType").GetString().Should().Be("ColorList");
    }

    [Fact]
    public async Task Metadata_shape_is_null_when_nothing_is_selected()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        (await service.BuildMetadataShapeAsync(CatalogDatabaseFixture.FashionDomainId, []))
            .Should().BeNull("no selection and an empty shape both mean fixed fields only");
    }

    [Fact]
    public async Task Metadata_shape_deduplicates_repeated_keys()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var shape = await service.BuildMetadataShapeAsync(
            CatalogDatabaseFixture.FashionDomainId,
            ["colors", "colors", "  colors  "]);

        shape!.RootElement.GetProperty("fields").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Metadata_shape_rejects_a_key_the_domain_does_not_offer()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        // "spicy" is a Restaurant field; a Fashion business must not be able to
        // smuggle it in, or its products would carry metadata nothing can render.
        var act = async () => await service.BuildMetadataShapeAsync(
            CatalogDatabaseFixture.FashionDomainId,
            ["colors", "spicy"]);

        await act.Should().ThrowAsync<UnknownProductAttributeException>();
    }

    [Fact]
    public async Task Metadata_shape_rejects_an_entirely_invented_key()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var act = async () => await service.BuildMetadataShapeAsync(
            CatalogDatabaseFixture.FashionDomainId,
            ["totallyMadeUpField"]);

        await act.Should().ThrowAsync<UnknownProductAttributeException>();
    }

    [Fact]
    public async Task Metadata_shape_persists_to_the_business_as_real_json()
    {
        var business = await _fixture.CreateBusinessAsync(
            "Shape Persist Co", CatalogDatabaseFixture.FashionDomainId);

        var service = CreateService(out var db);
        await using var _ = db;

        var shape = await service.BuildMetadataShapeAsync(
            CatalogDatabaseFixture.FashionDomainId,
            ["colors", "sizes"]);

        await using (var save = _fixture.CreateContext())
        {
            var tracked = await save.Businesses.FirstAsync(b => b.Id == business.Id);
            tracked.MetadataShape = shape;
            await save.SaveChangesAsync();
        }

        await using var verify = _fixture.CreateContext();

        var reloaded = await verify.Businesses.AsNoTracking().FirstAsync(b => b.Id == business.Id);

        reloaded.MetadataShape.Should().NotBeNull();
        reloaded.MetadataShape!.RootElement.GetProperty("fields")
            .EnumerateArray().Select(f => f.GetProperty("key").GetString())
            .Should().Equal(["colors", "sizes"]);
    }

    [Fact]
    public async Task Registration_persists_business_domain_and_custom_categories_atomically()
    {
        // Exercises the real transactional path AuthService uses, including the
        // invitation claim, rather than only the in-memory entity building above.
        await using var arrange = _fixture.CreateContext();

        var systemRoleId = await arrange.SystemRoles
            .Where(r => r.Role == SystemRole.User).Select(r => r.Id).FirstAsync();
        var businessRoleId = await arrange.BusinessUserRoles
            .Where(r => r.Role == BusinessRole.Owner).Select(r => r.Id).FirstAsync();

        var inviter = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Inviter",
            LastName = "Admin",
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = "x",
            SystemRoleId = systemRoleId,
        };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.test",
            TokenHash = Guid.NewGuid().ToString("N"),
            Type = InvitationType.BusinessOwner,
            CreatedByUserId = inviter.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        arrange.Users.Add(inviter);
        arrange.Invitations.Add(invitation);
        await arrange.SaveChangesAsync();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "New",
            LastName = "Owner",
            Email = invitation.Email,
            PasswordHash = "hashed",
            SystemRoleId = systemRoleId,
        };
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Atomic Co",
            OwnerUserId = owner.Id,
            BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
        };
        var businessUser = new BusinessUser
        {
            UserId = owner.Id,
            BusinessId = business.Id,
            RoleId = businessRoleId,
        };
        var customCategories = new List<Category>
        {
            new()
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = CatalogDatabaseFixture.FashionDomainId,
                BusinessId = business.Id,
                Name = "Upcycled",
                Slug = "upcycled",
                DisplayOrder = 100,
            },
        };

        await using (var act = _fixture.CreateContext())
        {
            await new UserRepository(act).FinishBusinessOwnerRegistration(
                owner, business, businessUser, customCategories, invitation.Id);
        }

        await using var verify = _fixture.CreateContext();

        var saved = await verify.Businesses.FirstAsync(b => b.Id == business.Id);
        saved.BusinessDomainId.Should().Be(CatalogDatabaseFixture.FashionDomainId);

        var savedCategory = await verify.Categories
            .SingleAsync(c => c.BusinessId == business.Id);
        savedCategory.Slug.Should().Be("upcycled");

        (await verify.Invitations.FirstAsync(i => i.Id == invitation.Id))
            .AcceptedAt.Should().NotBeNull("the invitation is claimed in the same transaction");
    }
}
