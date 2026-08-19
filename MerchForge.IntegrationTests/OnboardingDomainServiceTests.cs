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
            .Should().BeEquivalentTo(["electronics", "fashion", "restaurant"]);
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
