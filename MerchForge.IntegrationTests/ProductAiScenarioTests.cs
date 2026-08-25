using System.Text.Json;
using FluentAssertions;
using MerchForge.api.DTOs.ProductAi;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.ProductAi;
using MerchForge.api.Services.Subscription;
using MerchForge.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// Realistic multi-turn conversations, driven through a scripted provider.
///
/// The agent's own comprehension is exercised separately against the live model.
/// What these protect is everything around it: that state accumulates across turns,
/// that the backend re-decides completeness, that transitions are legal, that a
/// draft never escapes its business, and that a product is written exactly once and
/// only on explicit confirmation.
///
/// Business config here mirrors the spec: required colors + sizes with closed value
/// sets, optional material + brand, and a required image.
/// </summary>
public class ProductAiScenarioTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _fashion = null!;
    private Business _food = null!;
    private readonly Guid _ownerA = Guid.NewGuid();
    private readonly Guid _ownerB = Guid.NewGuid();

    public ProductAiScenarioTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _fashion = await _fixture.CreateBusinessAsync("Scenario Fashion", CatalogDatabaseFixture.FashionDomainId);
        _food = await _fixture.CreateBusinessAsync("Scenario Food", CatalogDatabaseFixture.RestaurantDomainId);

        await using var db = _fixture.CreateContext();

        var fashion = await db.Businesses.FirstAsync(b => b.Id == _fashion.Id);
        fashion.MetadataShape = JsonDocument.Parse("""
            {"fields":[
              {"key":"colors","label":"Colors","valueType":"TextList","isRequired":true,
               "allowedValues":["Black","White","Red","Blue","Green"]},
              {"key":"sizes","label":"Sizes","valueType":"TextList","isRequired":true,
               "allowedValues":["XS","S","M","L","XL","XXL"]},
              {"key":"material","label":"Material","valueType":"Text","isRequired":false,"allowedValues":[]},
              {"key":"brand","label":"Brand","valueType":"Text","isRequired":false,"allowedValues":[]}
            ]}
            """);

        // Business B: a completely different vocabulary. Nothing about colour or size.
        var food = await db.Businesses.FirstAsync(b => b.Id == _food.Id);
        food.MetadataShape = JsonDocument.Parse("""
            {"fields":[
              {"key":"flavor","label":"Flavor","valueType":"Text","isRequired":true,"allowedValues":[]},
              {"key":"weight","label":"Weight","valueType":"Text","isRequired":true,"allowedValues":[]},
              {"key":"origin","label":"Origin","valueType":"Text","isRequired":false,"allowedValues":[]}
            ]}
            """);

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---- harness ----

    private sealed class Harness : IDisposable
    {
        public required api.Data.MerchForgeDbContext Db { get; init; }
        public required ProductAiService Service { get; init; }
        public required FakeProductAiConversationClient Ai { get; init; }
        public required FakeAiTranscriptionService Transcription { get; init; }
        public required RecordingAiInteractionLogger Logger { get; init; }

        public void Dispose() => Db.Dispose();
    }

    private Harness CreateHarness()
    {
        var db = _fixture.CreateContext();
        var ai = new FakeProductAiConversationClient();
        var transcription = new FakeAiTranscriptionService();
        var logger = new RecordingAiInteractionLogger();

        var repo = new BusinessDashboardRepository(db);
        var dashboard = new BusinessDashboardService(repo, new SubscriptionRepository(db), new FakeBackgroundJobClient());

        var featureCreditRepo = new FeatureCreditRepository(db);
        var subscriptionService = new SubscriptionService(new SubscriptionRepository(db), featureCreditRepo);
        var featureCreditService = new FeatureCreditService(featureCreditRepo, subscriptionService);

        return new Harness
        {
            Db = db,
            Ai = ai,
            Transcription = transcription,
            Logger = logger,
            Service = new ProductAiService(
                new ProductDraftRepository(db), repo, dashboard,
                ai, transcription, new FakeProductImageService(), logger, featureCreditService),
        };
    }

    private static ProductAiDecision D(
        ProductAiAction action,
        string message = "ok",
        ProductAiDraft? draft = null) => new()
        {
            Action = action,
            Message = message,
            Draft = draft,
            MissingFields = [],
        };

    private static ProductAiDraft Draft(
        string? title = null,
        string? description = null,
        decimal? price = null,
        Guid? categoryId = null,
        string? metadataJson = null,
        decimal? compareAtPrice = null,
        string? sku = null,
        int? stockQuantity = null,
        List<string>? tags = null,
        DateTime? saleEndsAt = null) => new()
        {
            Title = title,
            Description = description,
            Price = price,
            CompareAtPrice = compareAtPrice,
            CategoryId = categoryId,
            Sku = sku,
            StockQuantity = stockQuantity,
            Tags = tags ?? [],
            SaleEndsAt = saleEndsAt,
            Metadata = metadataJson is null
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadataJson),
        };

    private static IFormFile Png() =>
        new FormFile(new MemoryStream([1, 2, 3, 4]), 0, 4, "file", "p.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };

    private static readonly Guid Shirts = CatalogDatabaseFixture.ShirtsCategoryId;
    private static readonly Guid Pizza = CatalogDatabaseFixture.PizzaCategoryId;

    private const string CompleteMetadata = """{"colors":["Black"],"sizes":["M","L"]}""";

    // =====================================================================
    // 1 / 3 / 4 / 12 — information arriving across turns and out of order
    // =====================================================================

    [Fact]
    public async Task Scenario01_happy_path_accumulates_across_turns_and_only_completes_at_the_end()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        // "I want to add a black hoodie for $35."
        h.Ai.Enqueue(D(ProductAiAction.RequestInformation, "What sizes, and can you send a photo?",
            Draft("Black Hoodie", "A black hoodie.", 35m, Shirts, """{"colors":["Black"]}""")));
        var t1 = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "I want to add a black hoodie for $35.");

        t1.Draft!.Price.Should().Be(35m);
        t1.MissingFields.Should().Contain(["image", "metadata.sizes"]);
        t1.CanConfirm.Should().BeFalse();

        // Image arrives.
        h.Ai.Enqueue(D(ProductAiAction.RequestInformation, "Great. Which sizes?"));
        var t2 = await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        t2.OriginalImageUrl.Should().NotBeNull();
        t2.MissingFields.Should().NotContain("image");
        // Everything gathered so far survives an image turn.
        t2.Draft!.Price.Should().Be(35m);
        t2.CanConfirm.Should().BeFalse("sizes are still missing");

        // "Sizes are M, L and XL."
        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "All set - here it is.",
            Draft("Black Hoodie", "A black hoodie.", 35m, Shirts,
                """{"colors":["Black"],"sizes":["M","L","XL"]}""")));
        var t3 = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "Sizes are M, L and XL.");

        t3.Status.Should().Be(nameof(ProductDraftStatus.WaitingForProductApproval));
        t3.MissingFields.Should().BeEmpty();
        t3.CanConfirm.Should().BeTrue();

        // Ready for review is not creation.
        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario12_order_of_information_does_not_matter()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        // Price, then sizes, then colour/title, then image - deliberately backwards.
        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Noted.", Draft(price: 60m)));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "The price is $60.");

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Noted.",
            Draft(price: 60m, metadataJson: """{"sizes":["M","L"]}""")));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "It's available in M and L.");

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Noted.",
            Draft("Red Hoodie", "A red hoodie.", 60m, Shirts,
                """{"sizes":["M","L"],"colors":["Red"]}""")));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "It's a red hoodie.");

        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "Ready."));
        var final = await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        final.Draft!.Price.Should().Be(60m);
        final.Draft.Title.Should().Be("Red Hoodie");
        final.MissingFields.Should().BeEmpty();
        final.CanConfirm.Should().BeTrue();
    }

    // =====================================================================
    // 8 / 37 / 38 / 39 — replacement semantics
    // =====================================================================

    [Fact]
    public async Task Scenario08_removing_a_size_replaces_the_list_rather_than_appending()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Got it.",
            Draft("Hoodie", "A hoodie.", 40m, Shirts, """{"colors":["Black"],"sizes":["M","L","XL"]}""")));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "Black hoodie, $40, sizes M L XL.");

        // The agent returns the whole state with XL gone - the contract is replacement,
        // so the stored list must shrink rather than accumulate.
        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Removed XL.",
            Draft("Hoodie", "A hoodie.", 40m, Shirts, """{"colors":["Black"],"sizes":["M","L"]}""")));
        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "Actually, don't list XL.");

        var sizes = after.Draft!.Metadata!.RootElement.GetProperty("sizes")
            .EnumerateArray().Select(e => e.GetString()).ToList();

        sizes.Should().Equal(["M", "L"]);
        sizes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Scenario37_repeating_the_same_information_does_not_duplicate_it()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        var state = Draft("Hoodie", "A hoodie.", 40m, Shirts, """{"colors":["Black"],"sizes":["M","L"]}""");

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Got it.", state));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "Black hoodie, $40, M L.");

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Already have that.", state));
        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "Also black, $40, M and L.");

        var meta = after.Draft!.Metadata!.RootElement;
        meta.GetProperty("colors").EnumerateArray().Should().HaveCount(1);
        meta.GetProperty("sizes").EnumerateArray().Select(e => e.GetString()).Should().Equal(["M", "L"]);
        after.Draft.Price.Should().Be(40m);
    }

    // =====================================================================
    // 10 / 11 / 53 — the backend decides completeness
    // =====================================================================

    [Fact]
    public async Task Scenario10_missing_required_fields_are_reported_precisely()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.RequestInformation, "What's the price and which sizes?"));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        h.Ai.Enqueue(D(ProductAiAction.RequestInformation, "Price and sizes?",
            Draft("Black Hoodie", "A black hoodie.", null, Shirts, """{"colors":["Black"]}""")));
        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "Black hoodie.");

        after.MissingFields.Should().BeEquivalentTo(["price", "metadata.sizes"]);
        after.CanConfirm.Should().BeFalse();
        after.Status.Should().Be(nameof(ProductDraftStatus.WaitingForMissingInformation));
    }

    [Fact]
    public async Task Scenario53_the_agent_claiming_completion_does_not_make_it_complete()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        // The agent insists it is done while a required metadata field is absent.
        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "All done, ready to create!",
            Draft("Hoodie", "A hoodie.", 40m, Shirts, """{"colors":["Black"]}""")));
        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "that's everything");

        after.MissingFields.Should().Contain("metadata.sizes");
        after.CanConfirm.Should().BeFalse();

        var act = async () => await h.Service.ConfirmAsync(_fashion.Id, _ownerA, d.Id);
        await act.Should().ThrowAsync<ProductDraftStateException>();

        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario54_a_hallucinated_value_never_reaches_the_products_table()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        // The agent claims the product is finished, with a colour that does not exist
        // for this business.
        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "Ready.",
            Draft("Hoodie", "A hoodie.", 40m, Shirts, """{"colors":["Purple"],"sizes":["M"]}""")));
        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "it's purple");

        // Caught where it was proposed, so the owner never sees it in the preview and
        // confirmation is simply unavailable.
        after.CanConfirm.Should().BeFalse();
        after.MissingFields.Should().Contain("metadata.colors");

        var act = async () => await h.Service.ConfirmAsync(_fashion.Id, _ownerA, d.Id);
        await act.Should().ThrowAsync<ProductDraftStateException>();

        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario54b_creation_still_refuses_a_disallowed_value_that_bypassed_the_conversation()
    {
        using var h = CreateHarness();
        var d = await ReachReviewAsync(h);

        // Writing straight to the draft stands in for any path that skips the turn
        // handler - a tampered record, or a future code path that forgets to strip.
        // Creation has to hold the line on its own.
        await using (var tamper = _fixture.CreateContext())
        {
            var stored = await tamper.ProductDrafts.FirstAsync(x => x.Id == d);
            stored.Draft = JsonDocument.Parse(
                "{\"title\":\"Hoodie\",\"description\":\"A hoodie.\",\"price\":40,"
                + $"\"categoryId\":\"{Shirts}\","
                + "\"metadata\":{\"colors\":[\"Purple\"],\"sizes\":[\"M\"]}}");
            await tamper.SaveChangesAsync();
        }

        using var attempt = CreateHarness();
        var act = async () => await attempt.Service.ConfirmAsync(_fashion.Id, _ownerA, d);

        var ex = (await act.Should().ThrowAsync<api.Exceptions.BusinessDashboard.InvalidProductMetadataException>()).Which;
        ex.Message.Should().Contain("Purple");

        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);
    }

    // =====================================================================
    // 16 / 17 / 20 / 21 — cancelling, rejecting, editing after preview
    // =====================================================================

    [Fact]
    public async Task Scenario16_changing_their_mind_cancels_without_creating_anything()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Got it.",
            Draft("Hoodie", "A hoodie.", 50m, Shirts, """{"colors":["Black"],"sizes":["M","L"]}""")));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "black hoodie $50, sizes M and L");

        h.Ai.Enqueue(D(ProductAiAction.Cancel, "No problem, I've discarded it."));
        var cancelled = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "Actually forget this product.");

        cancelled.Status.Should().Be(nameof(ProductDraftStatus.Cancelled));
        cancelled.CanConfirm.Should().BeFalse();

        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario17_restarting_after_cancelling_carries_nothing_over()
    {
        using var h = CreateHarness();

        var first = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Got it.",
            Draft("Black Hoodie", "A black hoodie.", 99m, Shirts, """{"colors":["Black"]}""")));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, first.Id, "black hoodie");

        await h.Service.CancelAsync(_fashion.Id, _ownerA, first.Id);

        // A restart is a new draft, so nothing from the abandoned one can leak in.
        var second = await h.Service.StartAsync(_fashion.Id, _ownerA);

        second.Id.Should().NotBe(first.Id);
        second.Draft.Should().BeNull();
        second.Messages.Should().ContainSingle("only the greeting");
        second.Status.Should().Be(nameof(ProductDraftStatus.CollectingInformation));
    }

    [Fact]
    public async Task Scenario21_editing_after_preview_updates_state_and_still_requires_confirmation()
    {
        using var h = CreateHarness();
        var d = await ReachReviewAsync(h, price: 40m);

        var preview = await h.Service.GetAsync(_fashion.Id, d);
        preview.Draft!.Price.Should().Be(40m);
        preview.CanConfirm.Should().BeTrue();

        // "Change the price to $45."
        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "Updated to $45.",
            Draft("Hoodie", "A hoodie.", 45m, Shirts, CompleteMetadata)));
        var edited = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d, "Change the price to $45.");

        edited.Draft!.Price.Should().Be(45m);
        edited.Status.Should().Be(nameof(ProductDraftStatus.WaitingForProductApproval));

        // Still not created - a preview edit does not confirm.
        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);

        var product = await h.Service.ConfirmAsync(_fashion.Id, _ownerA, d);
        product.Price.Should().Be(45m, "the edit, not the original price, is what gets created");
    }

    // =====================================================================
    // 22 / 24 / 25 / 26 / 27 / 28 — image workflow vs product workflow
    // =====================================================================

    [Fact]
    public async Task Scenario24_a_photo_request_alongside_a_product_change_does_not_block_the_change()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Got it.",
            Draft("Hoodie", "A hoodie.", 40m, Shirts, CompleteMetadata)));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "black hoodie $40 M L");

        // Photos are out of scope for this agent now, so a message that also brings
        // one up should still land the price change and leave the draft in the
        // ordinary conversation state — not waiting on anything image-related.
        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Got it.",
            Draft("Hoodie", "A hoodie.", 35m, Shirts, CompleteMetadata)));

        var after = await h.Service.SendMessageAsync(h.Transcription,
            _fashion.Id, _ownerA, d.Id, "Make the background white and change the price to $35.");

        after.Draft!.Price.Should().Be(35m);
        after.Status.Should().Be(nameof(ProductDraftStatus.CollectingInformation));
    }

    [Fact]
    public async Task Scenario27_approving_an_image_does_not_approve_the_product()
    {
        using var h = CreateHarness();
        var d = await ReachReviewWithPendingImageAsync(h);

        var approved = await h.Service.ResolveImageModificationAsync(_fashion.Id, _ownerA, d, approved: true);

        approved.OriginalImageUrl.Should().Be("/uploads/products/edited.png");
        approved.Status.Should().Be(nameof(ProductDraftStatus.CollectingInformation));

        // Approving a picture is not approving a product.
        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario28_a_product_cannot_be_confirmed_while_an_image_is_awaiting_approval()
    {
        using var h = CreateHarness();
        var d = await ReachReviewWithPendingImageAsync(h);

        var pending = await h.Service.GetAsync(_fashion.Id, d);

        // Everything else is complete, but the image question is still open.
        pending.CanConfirm.Should().BeFalse("an unresolved image blocks creation");

        var act = async () => await h.Service.SendMessageAsync(h.Transcription,
            _fashion.Id, _ownerA, d, "Everything else looks good, create it.");
        await act.Should().ThrowAsync<ProductDraftStateException>();
    }

    [Fact]
    public async Task Scenario28_confirming_directly_is_refused_while_an_image_is_pending()
    {
        using var h = CreateHarness();
        var d = await ReachReviewWithPendingImageAsync(h);

        // The UI hides the button via canConfirm, but the endpoint is reachable on its
        // own - a stale tab, a retry, or anyone calling the API directly. The rule has
        // to live in the service, not only in what the client chooses to render.
        var act = async () => await h.Service.ConfirmAsync(_fashion.Id, _ownerA, d);
        await act.Should().ThrowAsync<ProductDraftStateException>();

        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario26_rejecting_an_image_keeps_the_original_and_leaves_the_product_unfinalised()
    {
        using var h = CreateHarness();
        var d = await ReachReviewWithPendingImageAsync(h);

        var rejected = await h.Service.ResolveImageModificationAsync(_fashion.Id, _ownerA, d, approved: false);

        rejected.OriginalImageUrl.Should().Be("/uploads/products/uploaded.png");
        rejected.ProcessedImageUrl.Should().BeNull();
        rejected.ImageModificationPrompt.Should().BeNull();

        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);
    }

    // =====================================================================
    // 29 / 30 — off-topic input must not corrupt state
    // =====================================================================

    [Fact]
    public async Task Scenario30_an_off_topic_message_leaves_the_product_untouched()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        var established = Draft("Black Hoodie", "A black hoodie.", 40m, Shirts, """{"colors":["Black"]}""");

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Got it.", established));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "I want to add a black hoodie for $40.");

        // The agent answers without touching the draft, so state is unchanged.
        h.Ai.Enqueue(D(ProductAiAction.RequestInformation,
            "I can only help with your product. Which sizes does it come in?", established));
        var after = await h.Service.SendMessageAsync(h.Transcription,
            _fashion.Id, _ownerA, d.Id, "By the way, what's the weather today?");

        after.Draft!.Title.Should().Be("Black Hoodie");
        after.Draft.Description.Should().Be("A black hoodie.", "the question must not become the description");
        after.Draft.Price.Should().Be(40m);
    }

    // =====================================================================
    // 34 / 35 / 36 — business and user isolation
    // =====================================================================

    [Fact]
    public async Task Scenario34_a_food_business_is_never_given_clothing_fields()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_food.Id, _ownerB);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Got it."));
        await h.Service.SendMessageAsync(h.Transcription, _food.Id, _ownerB, d.Id, "This is vanilla flavor, 500 grams.");

        var context = h.Ai.ReceivedContexts.Single();

        context.MetadataFields.Select(f => f.Key).Should().BeEquivalentTo(["flavor", "weight", "origin"]);
        context.MetadataFields.Should().NotContain(f => f.Key == "colors" || f.Key == "sizes");

        // And its categories come from its own domain.
        context.Categories.Select(c => c.Name).Should().Contain("Pizza");
        context.Categories.Select(c => c.Name).Should().NotContain("Shirts");

        // The greeting never mentions clothing either.
        d.Messages[0].Text.Should().NotContain("color").And.NotContain("size");
    }

    [Fact]
    public async Task Scenario35_business_configurations_never_bleed_between_drafts()
    {
        using var h = CreateHarness();

        var fashionDraft = await h.Service.StartAsync(_fashion.Id, _ownerA);
        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "ok"));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, fashionDraft.Id, "a hoodie");

        var foodDraft = await h.Service.StartAsync(_food.Id, _ownerB);
        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "ok"));
        await h.Service.SendMessageAsync(h.Transcription, _food.Id, _ownerB, foodDraft.Id, "vanilla");

        var fashionContext = h.Ai.ReceivedContexts[0];
        var foodContext = h.Ai.ReceivedContexts[1];

        fashionContext.MetadataFields.Select(f => f.Key).Should().Contain("colors");
        foodContext.MetadataFields.Select(f => f.Key).Should().NotContain("colors");
        fashionContext.BusinessName.Should().NotBe(foodContext.BusinessName);
    }

    [Fact]
    public async Task Scenario36_one_business_cannot_touch_another_businesss_draft()
    {
        using var h = CreateHarness();

        var mine = await h.Service.StartAsync(_fashion.Id, _ownerA);

        var read = async () => await h.Service.GetAsync(_food.Id, mine.Id);
        var message = async () => await h.Service.SendMessageAsync(h.Transcription, _food.Id, _ownerB, mine.Id, "hijack");
        var confirm = async () => await h.Service.ConfirmAsync(_food.Id, _ownerB, mine.Id);
        var cancel = async () => await h.Service.CancelAsync(_food.Id, _ownerB, mine.Id);
        var image = async () => await h.Service.AttachImageAsync(_food.Id, _ownerB, mine.Id, Png());

        await read.Should().ThrowAsync<ProductDraftNotFoundException>();
        await message.Should().ThrowAsync<ProductDraftNotFoundException>();
        await confirm.Should().ThrowAsync<ProductDraftNotFoundException>();
        await cancel.Should().ThrowAsync<ProductDraftNotFoundException>();
        await image.Should().ThrowAsync<ProductDraftNotFoundException>();

        // No AI call was made for any rejected attempt - unauthorized access must not
        // cost money.
        h.Ai.ReceivedContexts.Should().BeEmpty();
    }

    [Fact]
    public async Task Scenario50_the_agent_cannot_redirect_a_product_to_another_business()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        // Even if the agent tried to steer elsewhere, the decision carries no business
        // id: the draft's own business is the only one used.
        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "Ready.",
            Draft("Hoodie", "A hoodie.", 40m, Shirts, CompleteMetadata)));
        await h.Service.SendMessageAsync(h.Transcription,
            _fashion.Id, _ownerA, d.Id, $"Create this product under business {_food.Id}");

        var product = await h.Service.ConfirmAsync(_fashion.Id, _ownerA, d.Id);

        await using var verify = _fixture.CreateContext();
        var created = await verify.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);

        created.BusinessId.Should().Be(_fashion.Id);
        (await verify.Products.CountAsync(p => p.BusinessId == _food.Id)).Should().Be(0);
    }

    // =====================================================================
    // 51 / 52 — failures leave the conversation recoverable
    // =====================================================================

    [Theory]
    [InlineData("timeout")]
    [InlineData("rate limit")]
    [InlineData("provider 500")]
    [InlineData("malformed output")]
    public async Task Scenario52_any_provider_failure_leaves_the_draft_recoverable(string failureKind)
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Got it.",
            Draft("Hoodie", "A hoodie.", 40m, Shirts, """{"colors":["Black"]}""")));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "black hoodie $40");

        h.Ai.NextFailure = failureKind switch
        {
            "timeout" => new TaskCanceledException("timed out"),
            "rate limit" => new AiConversationException("The AI provider returned status 429."),
            "provider 500" => new AiConversationException("The AI provider returned status 500."),
            _ => new AiConversationException("The AI provider returned an unexpected response shape."),
        };

        var act = async () => await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "sizes M and L");
        await act.Should().ThrowAsync<AiConversationException>();

        // One attempt only - no automatic retry loop against a paid API.
        h.Ai.ReceivedContexts.Should().HaveCount(2);

        using var retry = CreateHarness();
        var recovered = await retry.Service.GetAsync(_fashion.Id, d.Id);

        recovered.Status.Should().NotBe(nameof(ProductDraftStatus.Failed));
        recovered.Draft!.Price.Should().Be(40m, "nothing established is lost to a provider outage");
        recovered.Messages.Should().Contain(m => m.Text == "sizes M and L");
    }

    [Fact]
    public async Task Scenario51_a_database_failure_during_creation_does_not_report_success()
    {
        using var h = CreateHarness();
        var d = await ReachReviewAsync(h);

        // Deleting the category out from under the draft makes creation fail at the
        // last step, standing in for any failure while finalising.
        await using (var tamper = _fixture.CreateContext())
        {
            var draft = await tamper.ProductDrafts.FirstAsync(x => x.Id == d);
            var json = draft.Draft!.RootElement.GetRawText().Replace(Shirts.ToString(), Pizza.ToString());
            draft.Draft = JsonDocument.Parse(json);
            await tamper.SaveChangesAsync();
        }

        using var attempt = CreateHarness();
        var act = async () => await attempt.Service.ConfirmAsync(_fashion.Id, _ownerA, d);
        await act.Should().ThrowAsync<api.Exceptions.BusinessDashboard.InvalidProductCategoryException>();

        await using var verify = _fixture.CreateContext();
        var after = await verify.ProductDrafts.AsNoTracking().FirstAsync(x => x.Id == d);

        after.Status.Should().NotBe(ProductDraftStatus.Completed);
        after.ProductId.Should().BeNull();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(0);
    }

    // =====================================================================
    // 55 / 56 / 57 — confirmation is exactly-once
    // =====================================================================

    [Fact]
    public async Task Scenario57_a_retried_confirmation_after_a_lost_response_does_not_duplicate()
    {
        using var h = CreateHarness();
        var d = await ReachReviewAsync(h);

        // First call succeeds; imagine the response never reaches the client.
        await h.Service.ConfirmAsync(_fashion.Id, _ownerA, d);

        // The client retries.
        using var retry = CreateHarness();
        var act = async () => await retry.Service.ConfirmAsync(_fashion.Id, _ownerA, d);
        await act.Should().ThrowAsync<ProductDraftStateException>();

        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(1);
    }

    // =====================================================================
    // 58 / 59 — recovery and multiple drafts
    // =====================================================================

    [Fact]
    public async Task Scenario59_two_drafts_for_one_business_stay_independent()
    {
        using var h = CreateHarness();

        var hoodie = await h.Service.StartAsync(_fashion.Id, _ownerA);
        var shoes = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "ok", Draft("Hoodie", "A hoodie.", 40m, Shirts)));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, hoodie.Id, "a hoodie for $40");

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "ok",
            Draft("Shoes", "Some shoes.", 90m, CatalogDatabaseFixture.ShoesCategoryId)));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, shoes.Id, "shoes for $90");

        var hoodieState = await h.Service.GetAsync(_fashion.Id, hoodie.Id);
        var shoesState = await h.Service.GetAsync(_fashion.Id, shoes.Id);

        hoodieState.Draft!.Price.Should().Be(40m);
        shoesState.Draft!.Price.Should().Be(90m);
        hoodieState.Messages.Should().NotContain(m => m.Text.Contains("shoes"));
    }

    [Fact]
    public async Task Scenario58_a_draft_abandoned_mid_conversation_is_recoverable_in_full()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        h.Ai.Enqueue(D(ProductAiAction.RequestInformation, "Which sizes?",
            Draft("Black Hoodie", "A black hoodie.", 40m, Shirts, """{"colors":["Black"]}""")));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "black hoodie for $40");

        // Everything needed to resume: state, history, image, and the open question.
        using var later = CreateHarness();
        var resumed = await later.Service.GetAsync(_fashion.Id, d.Id);

        resumed.Draft!.Price.Should().Be(40m);
        resumed.OriginalImageUrl.Should().NotBeNull();
        resumed.Messages.Should().HaveCount(5);
        resumed.Messages.Last().Text.Should().Contain("Which sizes?");
        resumed.MissingFields.Should().Contain("metadata.sizes");
    }

    // =====================================================================
    // 46 — a long conversation
    // =====================================================================

    [Fact]
    public async Task Scenario46_a_long_conversation_keeps_the_final_state_correct()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        // Each turn restates the whole product with one thing changed, which is the
        // contract the agent works to.
        var turns = new (string User, ProductAiDraft State)[]
        {
            ("a hoodie", Draft("Hoodie", "A hoodie.", null, null, null)),
            ("it's black", Draft("Hoodie", "A hoodie.", null, null, """{"colors":["Black"]}""")),
            ("$20", Draft("Hoodie", "A hoodie.", 20m, null, """{"colors":["Black"]}""")),
            ("actually $25", Draft("Hoodie", "A hoodie.", 25m, null, """{"colors":["Black"]}""")),
            ("put it under shirts", Draft("Hoodie", "A hoodie.", 25m, Shirts, """{"colors":["Black"]}""")),
            ("sizes M and L", Draft("Hoodie", "A hoodie.", 25m, Shirts, """{"colors":["Black"],"sizes":["M","L"]}""")),
            ("add XL too", Draft("Hoodie", "A hoodie.", 25m, Shirts, """{"colors":["Black"],"sizes":["M","L","XL"]}""")),
            ("it's cotton", Draft("Hoodie", "A hoodie.", 25m, Shirts, """{"colors":["Black"],"sizes":["M","L","XL"],"material":"Cotton"}""")),
            ("actually $30", Draft("Hoodie", "A hoodie.", 30m, Shirts, """{"colors":["Black"],"sizes":["M","L","XL"],"material":"Cotton"}""")),
            ("drop XL", Draft("Hoodie", "A hoodie.", 30m, Shirts, """{"colors":["Black"],"sizes":["M","L"],"material":"Cotton"}""")),
            ("call it Winter Hoodie", Draft("Winter Hoodie", "A hoodie.", 30m, Shirts, """{"colors":["Black"],"sizes":["M","L"],"material":"Cotton"}""")),
            ("describe it as warm and soft", Draft("Winter Hoodie", "Warm and soft.", 30m, Shirts, """{"colors":["Black"],"sizes":["M","L"],"material":"Cotton"}""")),
            // Three extra no-op affirmations, purely to push this conversation past
            // VoiceHistoryTurnLimit (15) turns — otherwise the assertion below would
            // never actually observe the cap engaging.
            ("sounds good", Draft("Winter Hoodie", "Warm and soft.", 30m, Shirts, """{"colors":["Black"],"sizes":["M","L"],"material":"Cotton"}""")),
            ("yes that's right", Draft("Winter Hoodie", "Warm and soft.", 30m, Shirts, """{"colors":["Black"],"sizes":["M","L"],"material":"Cotton"}""")),
            ("great", Draft("Winter Hoodie", "Warm and soft.", 30m, Shirts, """{"colors":["Black"],"sizes":["M","L"],"material":"Cotton"}""")),
            ("final price is 27", Draft("Winter Hoodie", "Warm and soft.", 27m, Shirts, """{"colors":["Black"],"sizes":["M","L"],"material":"Cotton"}""")),
        };

        foreach (var (user, state) in turns)
        {
            h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Noted.", state));
            await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, user);
        }

        var final = await h.Service.GetAsync(_fashion.Id, d.Id);

        final.Draft!.Title.Should().Be("Winter Hoodie");
        final.Draft.Description.Should().Be("Warm and soft.");
        final.Draft.Price.Should().Be(27m, "the last explicit correction wins");

        var meta = final.Draft.Metadata!.RootElement;
        meta.GetProperty("sizes").EnumerateArray().Select(e => e.GetString()).Should().Equal(["M", "L"]);
        meta.GetProperty("material").GetString().Should().Be("Cotton");

        final.MissingFields.Should().BeEmpty();
        final.CanConfirm.Should().BeTrue();

        // History is capped for prompting (VoiceHistoryTurnLimit turns, 2 messages
        // each), but nothing established was lost.
        var lastContext = h.Ai.ReceivedContexts.Last();
        lastContext.History.Should().HaveCountLessThanOrEqualTo(30);
        lastContext.CurrentDraft!.Title.Should().Be("Winter Hoodie");
    }

    // =====================================================================
    // 60 — the full realistic conversation
    // =====================================================================

    [Fact]
    public async Task Scenario60_the_complete_conversation_creates_exactly_one_product()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        // "I want to add a new hoodie."
        h.Ai.Enqueue(D(ProductAiAction.RequestInformation, "Send a photo and tell me the price and sizes."));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "I want to add a new hoodie.");

        // Image + "It's a black cotton hoodie."
        h.Ai.Enqueue(D(ProductAiAction.RequestInformation, "Got it. Price and sizes?"));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        h.Ai.Enqueue(D(ProductAiAction.RequestInformation, "What's the price and which sizes?",
            Draft("Black Cotton Hoodie", "A black cotton hoodie.", null, Shirts,
                """{"colors":["Black"],"material":"Cotton"}""")));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "It's a black cotton hoodie.");

        // "$49 and comes in M, L and XL."
        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "Here's the product.",
            Draft("Black Cotton Hoodie", "A black cotton hoodie.", 49m, Shirts,
                """{"colors":["Black"],"material":"Cotton","sizes":["M","L","XL"]}""")));
        var priced = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "It's $49 and comes in M, L and XL.");
        priced.CanConfirm.Should().BeTrue();

        // "Actually change the price to $55."
        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "Updated.",
            Draft("Black Cotton Hoodie", "A black cotton hoodie.", 55m, Shirts,
                """{"colors":["Black"],"material":"Cotton","sizes":["M","L","XL"]}""")));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "Actually change the price to $55.");

        // An image edit lands (not through this conversation — a separate model owns
        // that — so it's simulated the same way anything outside the chat turn would
        // arrive: written straight onto the draft).
        var draft = await h.Db.ProductDrafts.FirstAsync(x => x.Id == d.Id);
        draft.ProcessedImageUrl = "/uploads/products/edited.png";
        draft.ImageModificationPrompt = "Replace the background with a clean white studio backdrop.";
        draft.Status = ProductDraftStatus.WaitingForImageApproval;
        await h.Db.SaveChangesAsync();

        var pendingImage = await h.Service.GetAsync(_fashion.Id, d.Id);

        pendingImage.Status.Should().Be(nameof(ProductDraftStatus.WaitingForImageApproval));
        pendingImage.CanConfirm.Should().BeFalse();

        // "Looks good." — image only.
        var imageApproved = await h.Service.ResolveImageModificationAsync(_fashion.Id, _ownerA, d.Id, approved: true);
        imageApproved.OriginalImageUrl.Should().Be("/uploads/products/edited.png");

        await using (var mid = _fixture.CreateContext())
        {
            (await mid.Products.CountAsync(p => p.BusinessId == _fashion.Id))
                .Should().Be(0, "approving the image is not approving the product");
        }

        // "Everything looks correct. Add it."
        var product = await h.Service.ConfirmAsync(_fashion.Id, _ownerA, d.Id);

        product.Title.Should().Be("Black Cotton Hoodie");
        product.Price.Should().Be(55m, "the correction, not the original price");
        product.ImageUrl.Should().Be("/uploads/products/edited.png", "the approved image");
        product.Metadata!.RootElement.GetProperty("sizes")
            .EnumerateArray().Select(e => e.GetString()).Should().Equal(["M", "L", "XL"]);

        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _fashion.Id)).Should().Be(1);

        var finalDraft = await verify.ProductDrafts.AsNoTracking().FirstAsync(x => x.Id == d.Id);
        finalDraft.Status.Should().Be(ProductDraftStatus.Completed);
        finalDraft.ProductId.Should().Be(product.Id);
    }


    [Fact]
    public async Task Scenario32_a_disallowed_value_never_enters_the_draft()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        // Observed against the live model: it sometimes returns a value outside the
        // configured set despite the prompt. The guarantee has to be ours.
        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Noted.",
            Draft("Hoodie", "A hoodie.", 40m, Shirts, """{"colors":["Purple"],"sizes":["M"]}""")));

        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "it's purple");

        after.Draft!.Metadata!.RootElement.TryGetProperty("colors", out _)
            .Should().BeFalse("an unsupported value is removed where it is proposed, not at creation");

        // Valid values in the same turn survive.
        after.Draft.Metadata.RootElement.GetProperty("sizes")
            .EnumerateArray().Select(e => e.GetString()).Should().Equal(["M"]);

        // And the owner is told, with the values they can actually use.
        after.Messages.Last().Text.Should().Contain("Purple").And.Contain("Black");
        after.MissingFields.Should().Contain("metadata.colors");
        h.Logger.Events.Should().Contain("rejected:metadata_value_not_allowed");
    }

    [Fact]
    public async Task Scenario33_only_the_unsupported_size_is_dropped()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Noted.",
            Draft("Hoodie", "A hoodie.", 40m, Shirts,
                """{"colors":["Black"],"sizes":["M","L","XXXXL"]}""")));

        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "sizes M, L, XXXXL");

        after.Draft!.Metadata!.RootElement.GetProperty("sizes")
            .EnumerateArray().Select(e => e.GetString())
            .Should().Equal(["M", "L"], "the valid sizes are kept rather than the whole turn being lost");

        after.Messages.Last().Text.Should().Contain("XXXXL");
    }

    /// <summary>
    /// ColorList has no allowedValues in practice - any colour is normally fine - so
    /// the closed-set check above never fires for it. What has to hold instead is the
    /// value shape: ProductMetadataBuilder requires hex codes and throws on anything
    /// else, so a colour name reaching ConfirmAsync would crash product creation
    /// instead of failing gracefully. This is caught here, the same turn it's proposed.
    /// </summary>
    [Fact]
    public async Task Scenario62_a_color_name_is_stripped_from_a_hex_only_field()
    {
        await using (var db = _fixture.CreateContext())
        {
            var fashion = await db.Businesses.FirstAsync(b => b.Id == _fashion.Id);
            fashion.MetadataShape = JsonDocument.Parse("""
                {"fields":[
                  {"key":"colors","label":"Colors","valueType":"ColorList","isRequired":true,"allowedValues":[]},
                  {"key":"sizes","label":"Sizes","valueType":"TextList","isRequired":true,
                   "allowedValues":["XS","S","M","L","XL","XXL"]}
                ]}
                """);
            await db.SaveChangesAsync();
        }

        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Noted.",
            Draft("Hoodie", "A hoodie.", 40m, Shirts,
                """{"colors":["#000000","white"],"sizes":["M"]}""")));

        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "black and white");

        after.Draft!.Metadata!.RootElement.GetProperty("colors")
            .EnumerateArray().Select(e => e.GetString())
            .Should().Equal(["#000000"], "the hex value survives; the colour name does not");

        after.Messages.Last().Text.Should().Contain("white").And.Contain("hex");
    }

    [Fact]
    public async Task Scenario61_fixed_attributes_beyond_the_basics_reach_the_created_product()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        var saleEndsAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "All set - here it is.",
            Draft("Hoodie", "A hoodie.", 45m, Shirts, CompleteMetadata,
                compareAtPrice: 60m,
                sku: "HD-BLK-M",
                stockQuantity: 12,
                tags: ["New", "Bestseller"],
                saleEndsAt: saleEndsAt)));
        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "that's everything, it was $60 now $45");

        // Visible in the chat's own draft preview before confirmation, same as every
        // other field - this is what a "product so far" preview showing tags or stock
        // is actually backed by.
        after.Draft!.CompareAtPrice.Should().Be(60m);
        after.Draft.Sku.Should().Be("HD-BLK-M");
        after.Draft.StockQuantity.Should().Be(12);
        after.Draft.Tags.Should().BeEquivalentTo(["New", "Bestseller"]);
        after.Draft.SaleEndsAt.Should().Be(saleEndsAt);

        var product = await h.Service.ConfirmAsync(_fashion.Id, _ownerA, d.Id);

        product.CompareAtPrice.Should().Be(60m);
        product.Sku.Should().Be("HD-BLK-M");
        product.StockQuantity.Should().Be(12);
        product.Tags.Should().BeEquivalentTo(["New", "Bestseller"]);
        product.SaleEndsAt.Should().Be(saleEndsAt);
    }

    [Fact]
    public async Task An_empty_assistant_message_still_produces_a_reply()
    {
        using var h = CreateHarness();
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        // Observed against the live model: the schema requires a message but does not
        // stop it being empty, and a silent assistant reads as a broken chat.
        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "", Draft("Hoodie", "A hoodie.", 40m, Shirts)));

        var after = await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "a hoodie for $40");

        after.Messages.Last().Role.Should().Be("assistant");
        after.Messages.Last().Text.Should().NotBeNullOrWhiteSpace();
    }

    // ---- helpers ----


    private async Task<Guid> ReachReviewAsync(Harness h, decimal price = 40m)
    {
        var d = await h.Service.StartAsync(_fashion.Id, _ownerA);

        h.Ai.Enqueue(D(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_fashion.Id, _ownerA, d.Id, Png());

        h.Ai.Enqueue(D(ProductAiAction.ReadyForReview, "Ready.",
            Draft("Hoodie", "A hoodie.", price, Shirts, CompleteMetadata)));
        await h.Service.SendMessageAsync(h.Transcription, _fashion.Id, _ownerA, d.Id, "that's everything");

        return d.Id;
    }

    /// <summary>
    /// Puts a draft into WaitingForImageApproval directly, rather than through a
    /// conversation turn: the conversational agent no longer requests or edits images
    /// at all (a separate model owns that), but the approve/reject machinery around
    /// an in-flight edit is still real code, still reachable by whatever eventually
    /// writes into these same columns, and still worth testing on its own terms.
    /// </summary>
    private async Task<Guid> ReachReviewWithPendingImageAsync(Harness h)
    {
        var d = await ReachReviewAsync(h);

        var draft = await h.Db.ProductDrafts.FirstAsync(x => x.Id == d);
        draft.ProcessedImageUrl = "/uploads/products/edited.png";
        draft.ImageModificationPrompt = "Make the background white.";
        draft.Status = ProductDraftStatus.WaitingForImageApproval;
        await h.Db.SaveChangesAsync();

        return d;
    }
}
