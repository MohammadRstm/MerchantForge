using System.Text.Json;
using FluentAssertions;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.ProductAi;
using MerchForge.api.Services.Subscription;
using MerchForge.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// One realistic conversation driven through the whole stack with the real model:
/// service, draft persistence, prompt, provider, validation and creation.
///
/// The behaviour tests exercise the prompt in isolation and the scenario tests
/// exercise the orchestration with a scripted provider. Neither proves the two fit
/// together, which is what this covers - deliberately once, since it is the most
/// expensive test in the suite.
/// </summary>
[Collection("Live AI")]
public class LiveEndToEndConversationTest
    : IClassFixture<CatalogDatabaseFixture>, IClassFixture<LiveAgentFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _db;
    private readonly LiveAgentFixture _ai;

    private Business _business = null!;
    private readonly Guid _owner = Guid.NewGuid();

    public LiveEndToEndConversationTest(CatalogDatabaseFixture db, LiveAgentFixture ai)
    {
        _db = db;
        _ai = ai;
    }

    public async Task InitializeAsync()
    {
        _business = await _db.CreateBusinessAsync("Live Apparel", CatalogDatabaseFixture.FashionDomainId);

        await using var context = _db.CreateContext();
        var business = await context.Businesses.FirstAsync(b => b.Id == _business.Id);

        business.MetadataShape = JsonDocument.Parse("""
            {"fields":[
              {"key":"colors","label":"Colors","valueType":"TextList","isRequired":true,
               "allowedValues":["Black","White","Red","Blue","Green"]},
              {"key":"sizes","label":"Sizes","valueType":"TextList","isRequired":true,
               "allowedValues":["XS","S","M","L","XL","XXL"]},
              {"key":"material","label":"Material","valueType":"Text","isRequired":false,"allowedValues":[]}
            ]}
            """);

        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private (ProductAiService Service, IDisposable Scope) CreateService()
    {
        var context = _db.CreateContext();
        var repo = new BusinessDashboardRepository(context);
        var dashboard = new BusinessDashboardService(repo, new SubscriptionRepository(context), new FakeBackgroundJobClient());

        var featureCreditRepo = new FeatureCreditRepository(context);
        var subscriptionService = new SubscriptionService(new SubscriptionRepository(context), featureCreditRepo);
        var featureCreditService = new FeatureCreditService(featureCreditRepo, subscriptionService);

        // Everything real except file storage, which has no provider behind it yet.
        var service = new ProductAiService(
            new ProductDraftRepository(context),
            repo,
            dashboard,
            _ai.CreateClient(),
            new FakeAiTranscriptionService(),
            new FakeProductImageService(),
            new RecordingAiInteractionLogger(),
            featureCreditService);

        return (service, context);
    }

    private static IFormFile Png() =>
        new FormFile(new MemoryStream([1, 2, 3, 4]), 0, 4, "file", "hoodie.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };

    [SkippableFact]
    public async Task A_real_conversation_produces_exactly_one_correct_product()
    {
        Skip.IfNot(_ai.IsConfigured, "No AI provider configured.");

        var (service, scope) = CreateService();
        using var _s = scope;

        var draft = await service.StartAsync(_business.Id, _owner);

        // The owner uploads a photo and describes the product.
        await service.AttachImageAsync(_business.Id, _owner, draft.Id, Png());

        var described = await service.SendMessageAsync(
            _business.Id, _owner, draft.Id,
            "It's a black cotton hoodie, put it under Shirts. Forty nine dollars, in medium, large and extra large.");

        described.Draft.Should().NotBeNull();
        described.Draft!.Price.Should().Be(49m);
        described.Draft.CategoryName.Should().Be("Shirts");

        // A correction, mid-conversation.
        var corrected = await service.SendMessageAsync(
            _business.Id, _owner, draft.Id, "Actually change the price to $55.");

        corrected.Draft!.Price.Should().Be(55m);
        corrected.Draft.Title.Should().NotBeNullOrWhiteSpace("the correction must not wipe the product");

        var sizes = corrected.Draft.Metadata!.RootElement.GetProperty("sizes")
            .EnumerateArray().Select(e => e.GetString()).ToList();
        sizes.Should().BeEquivalentTo(["M", "L", "XL"], "sizes survive a price correction");

        // Everything required is present, so the backend is willing to create.
        corrected.MissingFields.Should().BeEmpty();
        corrected.CanConfirm.Should().BeTrue();

        // Nothing has been written yet.
        await using (var mid = _db.CreateContext())
        {
            (await mid.Products.CountAsync(p => p.BusinessId == _business.Id)).Should().Be(0);
        }

        // Explicit confirmation is what creates it.
        var product = await service.ConfirmAsync(_business.Id, _owner, draft.Id);

        product.Price.Should().Be(55m);
        product.CategoryName.Should().Be("Shirts");
        product.ImageUrl.Should().NotBeNull();
        product.Metadata!.RootElement.GetProperty("colors")
            .EnumerateArray().Select(e => e.GetString()).Should().Equal(["Black"]);

        await using var verify = _db.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _business.Id))
            .Should().Be(1, "exactly one product, from one confirmation");

        var finalDraft = await verify.ProductDrafts.AsNoTracking().FirstAsync(d => d.Id == draft.Id);
        finalDraft.Status.Should().Be(ProductDraftStatus.Completed);
        finalDraft.ProductId.Should().Be(product.Id);
    }

    [SkippableFact]
    public async Task An_unsupported_value_never_lands_in_the_draft_whatever_the_model_returns()
    {
        Skip.IfNot(_ai.IsConfigured, "No AI provider configured.");

        var (service, scope) = CreateService();
        using var _s = scope;

        var draft = await service.StartAsync(_business.Id, _owner);
        await service.AttachImageAsync(_business.Id, _owner, draft.Id, Png());

        var after = await service.SendMessageAsync(
            _business.Id, _owner, draft.Id,
            "It's a purple hoodie, forty dollars, size medium, put it under Shirts.");

        // Deterministic regardless of which way the model goes: if it asks about the
        // colour, none is set; if it returns Purple anyway - which it does on some runs
        // - the service strips it. Either path leaves no unsupported value behind.
        var colors = after.Draft?.Metadata is not null
            && after.Draft.Metadata.RootElement.TryGetProperty("colors", out var c)
                ? c.EnumerateArray().Select(e => e.GetString()!).ToList()
                : [];

        colors.Should().NotContain("Purple");
        colors.Should().OnlyContain(v => new[] { "Black", "White", "Red", "Blue", "Green" }.Contains(v));

        // Deliberately not asserted: that the model asks rather than substituting a
        // different allowed colour. Measured over repeated runs it does so only most of
        // the time, and no backend check can tell a substituted "Black" from a genuine
        // one. The guarantee here is that nothing outside the configured set is stored;
        // a wrong-but-legal value is caught by the owner reviewing the preview, which is
        // why confirmation is mandatory. See the report's limitations.
    }
}
