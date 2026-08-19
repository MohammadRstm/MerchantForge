using System.Text.Json;
using FluentAssertions;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.ProductAi;
using MerchForge.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// The AI product-creation workflow, driven through a scripted provider.
///
/// No test here makes a real AI call: what is being protected is the orchestration -
/// how a decision updates state, what is re-validated, which transitions are allowed,
/// and that drafts stay inside their business - none of which depends on a live model.
/// </summary>
public class ProductAiWorkflowTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _business = null!;
    private Business _rivalBusiness = null!;
    private readonly Guid _userId = Guid.NewGuid();

    public ProductAiWorkflowTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _business = await _fixture.CreateBusinessAsync("AI Fashion Co", CatalogDatabaseFixture.FashionDomainId);
        _rivalBusiness = await _fixture.CreateBusinessAsync("AI Rival Co", CatalogDatabaseFixture.FashionDomainId);

        await using var db = _fixture.CreateContext();

        var tracked = await db.Businesses.FirstAsync(b => b.Id == _business.Id);

        tracked.MetadataShape = JsonDocument.Parse("""
            {"fields":[
              {"key":"colors","label":"Colors","valueType":"TextList"},
              {"key":"material","label":"Material","valueType":"Text"}
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
        public required FakeProductImageEditor ImageEditor { get; init; }
        public required RecordingAiInteractionLogger Logger { get; init; }

        public void Dispose() => Db.Dispose();
    }

    private Harness CreateHarness()
    {
        var db = _fixture.CreateContext();

        var ai = new FakeProductAiConversationClient();
        var transcription = new FakeAiTranscriptionService();
        var imageEditor = new FakeProductImageEditor();
        var logger = new RecordingAiInteractionLogger();

        var dashboardRepository = new BusinessDashboardRepository(db);
        var dashboardService = new BusinessDashboardService(dashboardRepository, new SubscriptionRepository(db));

        return new Harness
        {
            Db = db,
            Ai = ai,
            Transcription = transcription,
            ImageEditor = imageEditor,
            Logger = logger,
            Service = new ProductAiService(
                new ProductDraftRepository(db),
                dashboardRepository,
                dashboardService,
                ai,
                transcription,
                imageEditor,
                new FakeProductImageService(),
                logger),
        };
    }

    private static ProductAiDecision Decision(
        ProductAiAction action,
        string message = "ok",
        ProductAiDraft? draft = null,
        List<string>? missing = null,
        string? imagePrompt = null) => new()
        {
            Action = action,
            Message = message,
            Draft = draft,
            MissingFields = missing ?? [],
            ImageModificationPrompt = imagePrompt,
        };

    private static ProductAiDraft CompleteDraft(Guid categoryId, decimal price = 25m) => new()
    {
        Title = "Cotton Shirt",
        Description = "A comfortable cotton shirt.",
        Price = price,
        CategoryId = categoryId,
        Metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            """{"colors":["Black","White"],"material":"Cotton"}"""),
    };

    private static IFormFile FakeFile(string name = "f.bin", string contentType = "application/octet-stream")
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    // ---- draft lifecycle ----

    [Fact]
    public async Task Start_creates_a_draft_and_greets_without_calling_the_model()
    {
        using var h = CreateHarness();

        var draft = await h.Service.StartAsync(_business.Id, _userId);

        draft.Status.Should().Be(nameof(ProductDraftStatus.CollectingInformation));
        draft.Messages.Should().ContainSingle().Which.Role.Should().Be("assistant");
        draft.CanConfirm.Should().BeFalse();

        // The greeting is identical every time, so paying a model call for it would
        // be pure waste.
        h.Ai.ReceivedContexts.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_greeting_mentions_this_businesss_configured_fields()
    {
        using var h = CreateHarness();

        var draft = await h.Service.StartAsync(_business.Id, _userId);

        draft.Messages[0].Text.Should().Contain("colors").And.Contain("material");
    }

    [Fact]
    public async Task Draft_survives_being_reloaded()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Got it.",
            new ProductAiDraft { Title = "Shirt", Price = 25m }));

        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "a shirt for $25");

        // A separate service instance, as a later request would be.
        using var reloaded = CreateHarness();
        var resumed = await reloaded.Service.GetAsync(_business.Id, started.Id);

        resumed.Draft!.Title.Should().Be("Shirt");
        resumed.Draft.Price.Should().Be(25m);
        resumed.Messages.Should().HaveCount(3, "greeting, the owner's message, and the reply");
    }

    // ---- state updates and corrections ----

    [Fact]
    public async Task Agent_receives_the_current_draft_so_it_edits_state_rather_than_rebuilding()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Got it.",
            new ProductAiDraft { Title = "Shirt", Price = 25m }));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "a shirt for $25");

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Updated."));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "actually make it $29");

        // The second turn must carry the accumulated state, otherwise the agent has
        // no way to know a correction is a correction.
        var second = h.Ai.ReceivedContexts[1];
        second.CurrentDraft.Should().NotBeNull();
        second.CurrentDraft!.Title.Should().Be("Shirt");
        second.CurrentDraft.Price.Should().Be(25m);
    }

    [Fact]
    public async Task Correcting_one_field_keeps_the_others()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Got it.",
            new ProductAiDraft { Title = "Shirt", Description = "Nice shirt.", Price = 25m }));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "a shirt for $25");

        // The agent returns the whole state with one field changed, which is the
        // contract - so the price moves and nothing else does.
        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Updated the price.",
            new ProductAiDraft { Title = "Shirt", Description = "Nice shirt.", Price = 29m }));

        var result = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "actually make it $29");

        result.Draft!.Price.Should().Be(29m);
        result.Draft.Title.Should().Be("Shirt");
        result.Draft.Description.Should().Be("Nice shirt.");
    }

    [Fact]
    public async Task Agent_is_told_which_categories_and_fields_this_business_has()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.RequestInformation, "Which category?"));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "a shirt");

        var context = h.Ai.ReceivedContexts.Single();

        context.Categories.Select(c => c.Name).Should().Contain(["Shoes", "Shirts"]);
        context.MetadataFields.Select(f => f.Key).Should().Equal(["colors", "material"]);
        context.MetadataFields.Single(f => f.Key == "colors").ValueType.Should().Be("TextList");
        context.LatestUserMessage.Should().Be("a shirt");
    }

    [Fact]
    public async Task Latest_message_is_not_duplicated_into_the_history()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Got it."));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "first");

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Got it."));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "second");

        var context = h.Ai.ReceivedContexts[1];

        // Sending it twice would make the agent answer the same turn twice over.
        context.LatestUserMessage.Should().Be("second");
        context.History.Should().NotContain(m => m.Text == "second");
        context.History.Should().Contain(m => m.Text == "first");
    }

    // ---- missing fields and confirmation gating ----

    [Fact]
    public async Task Draft_cannot_be_confirmed_while_required_fields_are_missing()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Got it.",
            new ProductAiDraft { Title = "Shirt", Price = 25m }));

        var result = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "a shirt for $25");

        result.CanConfirm.Should().BeFalse();

        var act = async () => await h.Service.ConfirmAsync(_business.Id, _userId, started.Id);
        await act.Should().ThrowAsync<ProductDraftStateException>();
    }

    [Fact]
    public async Task Backend_decides_completeness_not_the_agents_claim()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        // The agent insists it is ready while description and category are absent.
        h.Ai.Enqueue(Decision(ProductAiAction.ReadyForReview, "All set!",
            new ProductAiDraft { Title = "Shirt", Price = 25m }));

        var result = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "done");

        result.Status.Should().Be(nameof(ProductDraftStatus.WaitingForProductApproval));
        result.CanConfirm.Should().BeFalse("the products table is what is being written, so we decide");

        var act = async () => await h.Service.ConfirmAsync(_business.Id, _userId, started.Id);
        await act.Should().ThrowAsync<ProductDraftStateException>();
    }

    [Fact]
    public async Task Missing_fields_are_reported_when_the_agent_offers_none()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Got it.",
            new ProductAiDraft { Title = "Shirt" }));

        var result = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "a shirt");

        result.MissingFields.Should().BeEquivalentTo(["description", "price", "category"]);
    }

    // ---- hallucinated category ----

    [Fact]
    public async Task A_category_the_business_cannot_use_is_dropped_rather_than_stored()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        // Pizza belongs to the Restaurant domain; this is a Fashion business.
        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Got it.",
            new ProductAiDraft
            {
                Title = "Shirt",
                Description = "A shirt.",
                Price = 25m,
                CategoryId = CatalogDatabaseFixture.PizzaCategoryId,
            }));

        var result = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "a shirt");

        result.Draft!.CategoryId.Should().BeNull("an unusable category is dropped at the point it is proposed");
        result.CanConfirm.Should().BeFalse();
        h.Logger.Events.Should().Contain("rejected:category_not_usable");
    }

    // ---- confirmation ----

    [Fact]
    public async Task Confirming_creates_the_product_and_links_it_to_the_draft()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.ReadyForReview, "Ready.",
            CompleteDraft(CatalogDatabaseFixture.ShirtsCategoryId)));

        var reviewed = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "done");
        reviewed.CanConfirm.Should().BeTrue();

        var product = await h.Service.ConfirmAsync(_business.Id, _userId, started.Id);

        product.Title.Should().Be("Cotton Shirt");
        product.Price.Should().Be(25m);
        product.CategoryName.Should().Be("Shirts");

        // Metadata went through the same validation manual creation uses.
        product.Metadata!.RootElement.GetProperty("material").GetString().Should().Be("Cotton");
        product.Metadata.RootElement.GetProperty("colors").EnumerateArray()
            .Select(e => e.GetString()).Should().Equal(["Black", "White"]);

        var after = await h.Service.GetAsync(_business.Id, started.Id);
        after.Status.Should().Be(nameof(ProductDraftStatus.Completed));
        after.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task A_draft_can_only_be_confirmed_once()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.ReadyForReview, "Ready.",
            CompleteDraft(CatalogDatabaseFixture.ShirtsCategoryId)));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "done");

        await h.Service.ConfirmAsync(_business.Id, _userId, started.Id);

        // A double click or a stale tab must not produce a second product.
        var act = async () => await h.Service.ConfirmAsync(_business.Id, _userId, started.Id);
        await act.Should().ThrowAsync<ProductDraftStateException>();

        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _business.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Nothing_is_written_to_products_before_confirmation()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.ReadyForReview, "Ready.",
            CompleteDraft(CatalogDatabaseFixture.ShirtsCategoryId)));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "done");

        // The agent calling it ready is a proposal, not a creation.
        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _business.Id)).Should().Be(0);
    }

    // ---- cancellation ----

    [Fact]
    public async Task Cancelling_ends_the_conversation()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        var cancelled = await h.Service.CancelAsync(_business.Id, _userId, started.Id);
        cancelled.Status.Should().Be(nameof(ProductDraftStatus.Cancelled));

        var act = async () => await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "hello?");
        await act.Should().ThrowAsync<ProductDraftStateException>();
    }

    [Fact]
    public async Task The_agent_can_end_the_conversation_when_the_owner_asks_to_stop()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.Cancel, "No problem, cancelled."));

        var result = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "forget it");

        result.Status.Should().Be(nameof(ProductDraftStatus.Cancelled));
        result.CanConfirm.Should().BeFalse();
    }

    [Fact]
    public async Task A_completed_draft_cannot_be_cancelled()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.ReadyForReview, "Ready.",
            CompleteDraft(CatalogDatabaseFixture.ShirtsCategoryId)));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "done");
        await h.Service.ConfirmAsync(_business.Id, _userId, started.Id);

        var act = async () => await h.Service.CancelAsync(_business.Id, _userId, started.Id);
        await act.Should().ThrowAsync<ProductDraftStateException>();
    }

    // ---- business isolation ----

    [Fact]
    public async Task One_business_cannot_read_a_draft_belonging_to_another()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        var act = async () => await h.Service.GetAsync(_rivalBusiness.Id, started.Id);

        await act.Should().ThrowAsync<ProductDraftNotFoundException>();
    }

    [Fact]
    public async Task One_business_cannot_drive_or_confirm_another_businesss_draft()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.ReadyForReview, "Ready.",
            CompleteDraft(CatalogDatabaseFixture.ShirtsCategoryId)));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "done");

        var rivalUser = Guid.NewGuid();

        var message = async () => await h.Service.SendMessageAsync(_rivalBusiness.Id, rivalUser, started.Id, "mine now");
        var confirm = async () => await h.Service.ConfirmAsync(_rivalBusiness.Id, rivalUser, started.Id);
        var cancel = async () => await h.Service.CancelAsync(_rivalBusiness.Id, rivalUser, started.Id);

        await message.Should().ThrowAsync<ProductDraftNotFoundException>();
        await confirm.Should().ThrowAsync<ProductDraftNotFoundException>();
        await cancel.Should().ThrowAsync<ProductDraftNotFoundException>();

        // And no product was created under either business.
        await using var verify = _fixture.CreateContext();
        (await verify.Products.CountAsync(p => p.BusinessId == _rivalBusiness.Id)).Should().Be(0);
        (await verify.Products.CountAsync(p => p.BusinessId == _business.Id)).Should().Be(0);
    }

    [Fact]
    public async Task An_unknown_draft_reports_the_same_error_as_a_foreign_one()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        var foreign = await Record.ExceptionAsync(() => h.Service.GetAsync(_rivalBusiness.Id, started.Id));
        var missing = await Record.ExceptionAsync(() => h.Service.GetAsync(_rivalBusiness.Id, Guid.NewGuid()));

        // Identical, so one business cannot probe whether a draft id exists elsewhere.
        foreign.Should().BeOfType<ProductDraftNotFoundException>();
        missing.Should().BeOfType<ProductDraftNotFoundException>();
        foreign!.Message.Should().Be(missing!.Message);
    }

    // ---- voice ----

    [Fact]
    public async Task Voice_is_transcribed_before_the_agent_sees_it()
    {
        using var h = CreateHarness();
        h.Transcription.Transcript = "a shirt for twenty five dollars";

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Got it."));

        var result = await h.Service.SendVoiceMessageAsync(
            _business.Id, _userId, started.Id, FakeFile("note.webm", "audio/webm"));

        h.Transcription.CallCount.Should().Be(1);

        // The agent only ever sees text.
        h.Ai.ReceivedContexts.Single().LatestUserMessage.Should().Be("a shirt for twenty five dollars");

        // The transcript is what is stored, marked as having arrived by voice.
        var voiceMessage = result.Messages.Single(m => m.Kind == "voice");
        voiceMessage.Text.Should().Be("a shirt for twenty five dollars");
    }

    [Fact]
    public async Task An_unintelligible_voice_message_is_rejected_without_calling_the_agent()
    {
        using var h = CreateHarness();
        h.Transcription.Transcript = "   ";

        var started = await h.Service.StartAsync(_business.Id, _userId);

        var act = async () => await h.Service.SendVoiceMessageAsync(
            _business.Id, _userId, started.Id, FakeFile("note.webm", "audio/webm"));

        await act.Should().ThrowAsync<AiConversationException>();
        h.Ai.ReceivedContexts.Should().BeEmpty("there is nothing to send, so no paid call is made");
    }

    // ---- images ----

    [Fact]
    public async Task Attaching_an_image_tells_the_agent_it_now_has_one()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Nice photo."));

        var result = await h.Service.AttachImageAsync(_business.Id, _userId, started.Id, FakeFile("p.png", "image/png"));

        result.OriginalImageUrl.Should().Be("/uploads/products/uploaded.png");
        h.Ai.ReceivedContexts.Single().HasImage.Should().BeTrue();
    }

    [Fact]
    public async Task An_image_modification_request_waits_for_the_owners_approval()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_business.Id, _userId, started.Id, FakeFile("p.png", "image/png"));

        h.Ai.Enqueue(Decision(ProductAiAction.RequestImageModification, "I'll neutralise the background.",
            imagePrompt: "Make the background neutral"));

        var result = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "make the background neutral");

        result.Status.Should().Be(nameof(ProductDraftStatus.WaitingForImageApproval));
        result.ProcessedImageUrl.Should().Be("/uploads/products/edited.png");
        result.ImageModificationPrompt.Should().Be("Make the background neutral");

        // The edited image is a proposal; it is not the product's image yet.
        result.OriginalImageUrl.Should().Be("/uploads/products/uploaded.png");
        result.CanConfirm.Should().BeFalse("nothing else can happen until the image is resolved");
    }

    [Fact]
    public async Task Approving_an_edited_image_makes_it_the_products_image()
    {
        using var h = CreateHarness();
        var started = await StartWithPendingImageEditAsync(h);

        var result = await h.Service.ResolveImageModificationAsync(_business.Id, _userId, started, approved: true);

        result.OriginalImageUrl.Should().Be("/uploads/products/edited.png");
        result.ProcessedImageUrl.Should().BeNull();
        result.Status.Should().Be(nameof(ProductDraftStatus.CollectingInformation));
    }

    [Fact]
    public async Task Rejecting_an_edited_image_keeps_the_original_and_discards_the_edit()
    {
        using var h = CreateHarness();
        var started = await StartWithPendingImageEditAsync(h);

        var result = await h.Service.ResolveImageModificationAsync(_business.Id, _userId, started, approved: false);

        result.OriginalImageUrl.Should().Be("/uploads/products/uploaded.png");
        result.ProcessedImageUrl.Should().BeNull("a rejected edit is discarded, not kept around to be confused later");
        result.ImageModificationPrompt.Should().BeNull();
    }

    [Fact]
    public async Task A_failed_image_edit_does_not_strand_the_draft()
    {
        using var h = CreateHarness();
        h.ImageEditor.Failure = new InvalidOperationException("editor exploded");

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_business.Id, _userId, started.Id, FakeFile("p.png", "image/png"));

        h.Ai.Enqueue(Decision(ProductAiAction.RequestImageModification, "On it.",
            imagePrompt: "Make the background neutral"));

        var result = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "neutral background please");

        // ProcessingImage has no way out on its own, so a failure must not leave the
        // draft sitting in it.
        result.Status.Should().Be(nameof(ProductDraftStatus.CollectingInformation));
        result.ProcessedImageUrl.Should().BeNull();
        result.Messages.Last().Text.Should().Contain("couldn't edit the image");
    }

    [Fact]
    public async Task An_edit_request_with_no_editor_configured_says_so_and_continues()
    {
        using var h = CreateHarness();
        h.ImageEditor.IsAvailable = false;

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_business.Id, _userId, started.Id, FakeFile("p.png", "image/png"));

        h.Ai.Enqueue(Decision(ProductAiAction.RequestImageModification, "On it.",
            imagePrompt: "Make the background neutral"));

        var result = await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "neutral background please");

        result.Status.Should().Be(nameof(ProductDraftStatus.CollectingInformation));
        result.Messages.Last().Text.Should().Contain("can't edit images yet");
    }

    [Fact]
    public async Task Messages_are_blocked_while_an_image_is_awaiting_approval()
    {
        using var h = CreateHarness();
        var started = await StartWithPendingImageEditAsync(h);

        var act = async () => await h.Service.SendMessageAsync(_business.Id, _userId, started, "something else");

        await act.Should().ThrowAsync<ProductDraftStateException>();
    }

    private async Task<Guid> StartWithPendingImageEditAsync(Harness h)
    {
        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.Enqueue(Decision(ProductAiAction.UpdateDraft, "Nice photo."));
        await h.Service.AttachImageAsync(_business.Id, _userId, started.Id, FakeFile("p.png", "image/png"));

        h.Ai.Enqueue(Decision(ProductAiAction.RequestImageModification, "On it.",
            imagePrompt: "Make the background neutral"));
        await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "neutral background");

        return started.Id;
    }

    // ---- provider failure ----

    [Fact]
    public async Task A_provider_outage_keeps_the_conversation_resumable()
    {
        using var h = CreateHarness();

        var started = await h.Service.StartAsync(_business.Id, _userId);

        h.Ai.NextFailure = new HttpRequestException("provider down");

        var act = async () => await h.Service.SendMessageAsync(_business.Id, _userId, started.Id, "a shirt for $25");
        await act.Should().ThrowAsync<AiConversationException>();

        h.Logger.Events.Should().Contain("failed");

        // The draft stays usable: a transient outage must not end a conversation, and
        // the owner's message is kept so they don't have to retype it.
        using var retry = CreateHarness();
        var resumed = await retry.Service.GetAsync(_business.Id, started.Id);

        resumed.Status.Should().Be(nameof(ProductDraftStatus.CollectingInformation));
        resumed.Messages.Should().Contain(m => m.Text == "a shirt for $25");
    }
}
