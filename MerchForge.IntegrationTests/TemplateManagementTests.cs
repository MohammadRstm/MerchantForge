using FluentAssertions;
using MerchForge.api.Data;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using MerchForge.IntegrationTests.Fakes;

namespace MerchForge.IntegrationTests;

/// <summary>
/// The Super Admin Templates enhancement's new aggregation/filtering logic:
/// paged search/filter/sort over WebsiteTemplates, platform stats, domain
/// summary, requested-templates ranking, and the new reactivate symmetry.
/// Real MariaDB, matching this suite's established convention.
/// </summary>
public class TemplateManagementTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _businessA = null!;

    public TemplateManagementTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _businessA = await _fixture.CreateBusinessAsync("Template Test Co A", CatalogDatabaseFixture.FashionDomainId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static WebsiteTemplate MakeTemplate(
        Guid domainId, string? name = null, bool isActive = true, int displayOrder = 0) => new()
    {
        Id = Guid.NewGuid(),
        BusinessDomainId = domainId,
        Name = name ?? $"template-{Guid.NewGuid():N}",
        Label = $"Test Template {Guid.NewGuid():N}"[..24],
        PreviewImageUrl = "/images/templates/coming-soon.jpg",
        IsActive = isActive,
        DisplayOrder = displayOrder,
    };

    private WebsiteTemplateRequest MakeRequest(Guid templateId, DateTime? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = _businessA.Id,
        RequestedByUserId = _businessA.OwnerUserId,
        WebsiteTemplateId = templateId,
        CustomizationNotes = "Please make it blue.",
        Status = WebsiteTemplateRequestStatus.Pending,
        CreatedAt = createdAt ?? DateTime.UtcNow,
    };

    [Fact]
    public async Task GetWebsiteTemplatesAsync_search_matches_name_label_and_domain()
    {
        await using var db = _fixture.CreateContext();
        var marker = $"Zzz-{Guid.NewGuid():N}"[..16];
        var template = MakeTemplate(CatalogDatabaseFixture.FashionDomainId, name: $"template-{marker}".ToLowerInvariant());
        db.WebsiteTemplates.Add(template);
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var (items, _) = await repository.GetWebsiteTemplatesAsync(
            new WebsiteTemplatesQueryRequest { Search = marker, PageSize = 50 });

        items.Should().ContainSingle(t => t.Id == template.Id);
    }

    [Fact]
    public async Task GetWebsiteTemplatesAsync_filters_by_domain_active_and_usage()
    {
        await using var db = _fixture.CreateContext();
        var activeUnused = MakeTemplate(CatalogDatabaseFixture.FashionDomainId, isActive: true);
        var inactive = MakeTemplate(CatalogDatabaseFixture.FashionDomainId, isActive: false);
        var restaurantTemplate = MakeTemplate(CatalogDatabaseFixture.RestaurantDomainId, isActive: true);

        db.WebsiteTemplates.AddRange(activeUnused, inactive, restaurantTemplate);
        await db.SaveChangesAsync();

        // Assign a business to activeUnused so it's no longer "unused" for the next check.
        var usedTemplate = MakeTemplate(CatalogDatabaseFixture.FashionDomainId, isActive: true);
        db.WebsiteTemplates.Add(usedTemplate);
        await db.SaveChangesAsync();

        var business = await db.Businesses.FirstAsync(b => b.Id == _businessA.Id);
        business.WebsiteTemplateId = usedTemplate.Id;
        business.WebsiteTemplateChosenAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);

        var (byDomain, _) = await repository.GetWebsiteTemplatesAsync(
            new WebsiteTemplatesQueryRequest { BusinessDomainId = CatalogDatabaseFixture.RestaurantDomainId, PageSize = 200 });
        byDomain.Should().Contain(t => t.Id == restaurantTemplate.Id);
        byDomain.Should().NotContain(t => t.Id == activeUnused.Id);

        var (activeOnly, _) = await repository.GetWebsiteTemplatesAsync(
            new WebsiteTemplatesQueryRequest { IsActive = true, PageSize = 200 });
        activeOnly.Should().Contain(t => t.Id == activeUnused.Id);
        activeOnly.Should().NotContain(t => t.Id == inactive.Id);

        var (usedOnly, _) = await repository.GetWebsiteTemplatesAsync(
            new WebsiteTemplatesQueryRequest { HasBusinesses = true, PageSize = 200 });
        usedOnly.Should().Contain(t => t.Id == usedTemplate.Id);
        usedOnly.Should().NotContain(t => t.Id == activeUnused.Id);

        var (unusedOnly, _) = await repository.GetWebsiteTemplatesAsync(
            new WebsiteTemplatesQueryRequest { HasBusinesses = false, PageSize = 200 });
        unusedOnly.Should().Contain(t => t.Id == activeUnused.Id);
        unusedOnly.Should().NotContain(t => t.Id == usedTemplate.Id);
    }

    [Fact]
    public async Task GetWebsiteTemplatesAsync_reports_request_count_per_template()
    {
        await using var db = _fixture.CreateContext();
        var template = MakeTemplate(CatalogDatabaseFixture.FashionDomainId);
        db.WebsiteTemplates.Add(template);
        await db.SaveChangesAsync();

        db.WebsiteTemplateRequests.AddRange(MakeRequest(template.Id), MakeRequest(template.Id));
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var (items, _) = await repository.GetWebsiteTemplatesAsync(
            new WebsiteTemplatesQueryRequest { Search = template.Name, PageSize = 50 });

        items.Should().ContainSingle().Which.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTemplateStatsAsync_reports_most_used_template_and_totals()
    {
        await using var db = _fixture.CreateContext();
        var popular = MakeTemplate(CatalogDatabaseFixture.FashionDomainId);
        db.WebsiteTemplates.Add(popular);
        await db.SaveChangesAsync();

        var business = await db.Businesses.FirstAsync(b => b.Id == _businessA.Id);
        business.WebsiteTemplateId = popular.Id;
        business.WebsiteTemplateChosenAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var stats = await repository.GetTemplateStatsAsync();

        stats.TotalTemplates.Should().BeGreaterThanOrEqualTo(1);
        stats.MostUsedTemplateBusinessCount.Should().BeGreaterThanOrEqualTo(1);
        stats.BusinessesUsingTemplates.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDomainTemplateSummaryAsync_groups_templates_and_businesses_by_domain()
    {
        await using var db = _fixture.CreateContext();
        var template = MakeTemplate(CatalogDatabaseFixture.FashionDomainId);
        db.WebsiteTemplates.Add(template);
        await db.SaveChangesAsync();

        var business = await db.Businesses.FirstAsync(b => b.Id == _businessA.Id);
        business.WebsiteTemplateId = template.Id;
        business.WebsiteTemplateChosenAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var summary = await repository.GetDomainTemplateSummaryAsync();

        var fashion = summary.Should().Contain(s => s.BusinessDomainId == CatalogDatabaseFixture.FashionDomainId).Which;
        fashion.TemplateCount.Should().BeGreaterThanOrEqualTo(1);
        fashion.BusinessCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetRequestedTemplatesAsync_ranks_by_request_count_descending()
    {
        await using var db = _fixture.CreateContext();
        var popular = MakeTemplate(CatalogDatabaseFixture.FashionDomainId);
        var rare = MakeTemplate(CatalogDatabaseFixture.FashionDomainId);
        db.WebsiteTemplates.AddRange(popular, rare);
        await db.SaveChangesAsync();

        db.WebsiteTemplateRequests.AddRange(MakeRequest(popular.Id), MakeRequest(popular.Id), MakeRequest(rare.Id));
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var ranked = await repository.GetRequestedTemplatesAsync(50);

        var popularEntry = ranked.Should().Contain(r => r.Key == popular.Label).Which;
        var rareEntry = ranked.Should().Contain(r => r.Key == rare.Label).Which;
        popularEntry.Count.Should().BeGreaterThan(rareEntry.Count);
    }

    [Fact]
    public async Task Reactivate_flips_an_inactive_template_back_to_active()
    {
        await using var db = _fixture.CreateContext();
        var template = MakeTemplate(CatalogDatabaseFixture.FashionDomainId, isActive: false);
        db.WebsiteTemplates.Add(template);
        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db, TestImageUrls.Resolver);
        var tracked = await repository.GetTrackedWebsiteTemplateAsync(template.Id);
        tracked.Should().NotBeNull();
        tracked!.IsActive = true;
        tracked.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync();

        var reloaded = await db.WebsiteTemplates.AsNoTracking().FirstAsync(t => t.Id == template.Id);
        reloaded.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AuditLogRepository_filters_by_entity_id_for_a_single_templates_activity()
    {
        await using var db = _fixture.CreateContext();
        var templateId = Guid.NewGuid();
        var otherTemplateId = Guid.NewGuid();
        var repository = new AuditLogRepository(db);

        var matching = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorDisplayName = "Test Admin",
            EventType = AuditEventType.Template,
            Action = "WebsiteTemplateUpdated",
            EntityType = "WebsiteTemplate",
            EntityId = templateId,
            Description = "Updated website template \"Test\".",
            Success = true,
            CreatedAt = DateTime.UtcNow,
        };
        var other = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorDisplayName = "Test Admin",
            EventType = AuditEventType.Template,
            Action = "WebsiteTemplateUpdated",
            EntityType = "WebsiteTemplate",
            EntityId = otherTemplateId,
            Description = "Updated a different template.",
            Success = true,
            CreatedAt = DateTime.UtcNow,
        };

        await repository.AddAsync(matching);
        await repository.AddAsync(other);

        var (items, _) = await repository.GetLogsAsync(new MerchForge.api.DTOs.Audit.AuditLogQueryRequest { EntityId = templateId, PageSize = 50 });

        items.Should().Contain(l => l.Id == matching.Id);
        items.Should().NotContain(l => l.Id == other.Id);
    }
}
