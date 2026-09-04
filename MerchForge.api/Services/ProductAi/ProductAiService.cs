using System.Diagnostics;
using System.Text.Json;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.ProductAi;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.ProductAi.Interfaces;
using MerchForge.api.Services.Subscription.interfaces;
using MerchForge.api.Services.Storage.interfaces;

namespace MerchForge.api.Services.ProductAi
{
    public partial class ProductAiService : IProductAiService
    {
        /// <summary>
        /// How many past voice turns go to the agent. The structured draft already
        /// carries accumulated state, so history only needs to cover recent
        /// back-and-forth ("the second one", "no, the other colour") — this is a
        /// count of owner turns, not raw stored messages, since every owner turn is
        /// paired with one assistant reply.
        /// </summary>
        private const int VoiceHistoryTurnLimit = 15;

        private readonly IProductDraftRepository _draftRepository;
        private readonly IBusinessDashboardRepository _dashboardRepository;
        private readonly IBusinessDashboardService _dashboardService;
        private readonly IProductAiConversationClient _conversationClient;
        private readonly IAiTranscriptionService _transcription;
        private readonly IProductImageService _imageService;
        private readonly IAiInteractionLogger _aiLogger;
        private readonly IFeatureCreditService _featureCreditService;
        private readonly IProductImageUrlResolver _productImageUrlResolver;

        public ProductAiService(
            IProductDraftRepository draftRepository,
            IBusinessDashboardRepository dashboardRepository,
            IBusinessDashboardService dashboardService,
            IProductAiConversationClient conversationClient,
            IAiTranscriptionService transcription,
            IProductImageService imageService,
            IAiInteractionLogger aiLogger,
            IFeatureCreditService featureCreditService,
            IProductImageUrlResolver productImageUrlResolver)
        {
            _productImageUrlResolver = productImageUrlResolver;
            _draftRepository = draftRepository;
            _dashboardRepository = dashboardRepository;
            _dashboardService = dashboardService;
            _conversationClient = conversationClient;
            _transcription = transcription;
            _imageService = imageService;
            _aiLogger = aiLogger;
            _featureCreditService = featureCreditService;
        }

        // ---- lifecycle ----

        public async Task<ProductDraftResponse> StartAsync(
            Guid businessId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            // Confirms the business exists (and, with the route policy, that this user
            // belongs to it) before creating anything.
            var form = await _dashboardService.GetProductFormAsync(businessId, cancellationToken);

            var draft = new ProductDraft
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CreatedByUserId = userId,
                Status = ProductDraftStatus.CollectingInformation,
                LastMessageAt = DateTime.UtcNow,
            };

            // The opening prompt is written here, not requested from the agent: it is
            // the same every time, so paying for a model call to produce it would be
            // waste. The fields listed come from this business's configuration.
            ProductDraftState.AppendMessage(draft, "assistant", BuildGreeting(form), "text");

            await _draftRepository.CreateAsync(draft, cancellationToken);

            return await BuildResponseAsync(draft, [], cancellationToken, form);
        }

        public async Task<ProductDraftResponse> GetAsync(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken = default)
        {
            var draft = await LoadDraftAsync(businessId, draftId, cancellationToken);

            return await BuildResponseAsync(draft, [], cancellationToken);
        }

        public async Task<ProductDraftResponse> SendVoiceMessageAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            IFormFile audio,
            CancellationToken cancellationToken = default)
        {
            var draft = await LoadDraftAsync(businessId, draftId, cancellationToken);

            EnsureConversationOpen(draft);

            if (audio.Length == 0)
            {
                throw new AiConversationException("The voice message was empty.");
            }

            // Transcribed before the agent is involved: the orchestration never
            // handles audio, only the text that came out of it.
            await using var stream = audio.OpenReadStream();

            var transcript = await _transcription.TranscribeAsync(
                stream,
                audio.FileName,
                audio.ContentType ?? "application/octet-stream",
                cancellationToken);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                throw new AiConversationException("The voice message could not be understood.");
            }

            return await RunTurnAsync(draft, userId, transcript, "voice", cancellationToken);
        }

        public async Task<ProductDraftResponse> AttachImageAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            IFormFile image,
            CancellationToken cancellationToken = default)
        {
            var draft = await LoadDraftAsync(businessId, draftId, cancellationToken);

            EnsureConversationOpen(draft);

            // Reuses the same validated upload path as manual product creation, so an
            // image supplied through the agent gets identical signature checking.
            //
            // The draft's own id doubles as the product id in the image key, and
            // ConfirmAsync creates the product with that same id, so the key names the
            // product it really belongs to. A draft becomes exactly one product, which
            // is what makes reusing the id honest rather than convenient.
            var imageUrl = await _imageService.SaveAsync(businessId, draft.Id, image, cancellationToken);

            draft.OriginalImageUrl = imageUrl;

            // An image replaces whatever edit was previously in flight.
            draft.ProcessedImageUrl = null;
            draft.ImageModificationPrompt = null;

            // The agent is told an image arrived so it can stop asking for one and
            // move on to whatever is still missing.
            return await RunTurnAsync(draft, userId, "[The owner attached a product image.]", "image", cancellationToken);
        }

        public async Task<ProductDraftResponse> ResolveImageModificationAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            bool approved,
            CancellationToken cancellationToken = default)
        {
            var draft = await LoadDraftAsync(businessId, draftId, cancellationToken);

            if (draft.Status != ProductDraftStatus.WaitingForImageApproval)
            {
                throw new ProductDraftStateException("There is no image waiting for approval.");
            }

            if (approved)
            {
                // The edited image becomes the product's image.
                draft.OriginalImageUrl = draft.ProcessedImageUrl ?? draft.OriginalImageUrl;
                ProductDraftState.AppendMessage(draft, "assistant", "Great — I'll use the updated image.", "text");
            }
            else
            {
                // Rejecting keeps the original; the edit is discarded rather than kept
                // around to be confused with an approved one later.
                ProductDraftState.AppendMessage(draft, "assistant", "No problem — I've kept the original image.", "text");
            }

            draft.ProcessedImageUrl = null;
            draft.ImageModificationPrompt = null;
            draft.Status = ProductDraftStatus.CollectingInformation;
            draft.UpdatedAt = DateTime.UtcNow;

            await _draftRepository.SaveChangesAsync(cancellationToken);

            return await BuildResponseAsync(draft, [], cancellationToken);
        }

        public async Task<BusinessProductDetailResponse> ConfirmAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            CancellationToken cancellationToken = default)
        {
            var draft = await LoadDraftAsync(businessId, draftId, cancellationToken);

            // Guards a double click or a stale tab from creating the product twice.
            if (draft.Status == ProductDraftStatus.Completed)
            {
                throw new ProductDraftStateException("This draft has already been turned into a product.");
            }

            if (draft.Status is ProductDraftStatus.Cancelled or ProductDraftStatus.Failed)
            {
                throw new ProductDraftStateException("This draft is no longer active.");
            }

            // An unresolved image edit blocks creation. The UI already hides the button
            // via CanConfirm, but this endpoint is reachable on its own - a stale tab, a
            // retried request, or a direct API call - so the rule has to be enforced
            // here rather than trusted to the client. Otherwise a product could be
            // created carrying the pre-edit image the owner was still deciding about.
            if (draft.Status is ProductDraftStatus.WaitingForImageApproval
                or ProductDraftStatus.ProcessingImage)
            {
                throw new ProductDraftStateException(
                    "Please accept or reject the updated image before creating the product.");
            }

            var state = ProductDraftState.ReadDraft(draft);

            // Re-validated here rather than trusting the agent's ReadyForReview: the
            // product table is the thing being written, so completeness is decided by
            // us. Category usability and metadata typing are then enforced again by
            // the normal product path below.
            var formData = await _dashboardRepository.GetProductFormDataAsync(businessId, cancellationToken);

            var missing = FindMissingFields(state, draft.OriginalImageUrl is not null, formData?.MetadataShape);

            if (missing.Count > 0)
            {
                throw new ProductDraftStateException(
                    $"The product is still missing: {string.Join(", ", missing)}.");
            }

            var request = new SaveProductRequest
            {
                // Same id the draft's images were stored under, so
                // businesses/{b}/products/{draft.Id}/images/... describes where the
                // image actually ends up.
                Id = draft.Id,
                Title = state!.Title!,
                Description = state.Description,
                Price = state.Price!.Value,
                CompareAtPrice = state.CompareAtPrice,
                CategoryId = state.CategoryId!.Value,
                Sku = state.Sku,
                StockQuantity = state.StockQuantity,
                Tags = state.Tags,
                SaleEndsAt = state.SaleEndsAt,
                // The missing-fields check above already guarantees this is non-null —
                // a draft can't reach here without one. The AI flow only ever produces
                // a single image today, so it's the product's one and only (main) image.
                Images = [new ProductImageRequest { Url = draft.OriginalImageUrl!, IsMain = true }],
                Metadata = state.Metadata,
            };

            // Claimed before the product is created, in one atomic statement. The
            // status checks above are a fast, friendly rejection; this is what
            // actually makes concurrent confirmations exclusive, since two of them
            // would otherwise both pass those checks and create two products.
            var claimed = await _draftRepository.TryClaimForConfirmationAsync(
                businessId,
                draftId,
                cancellationToken);

            if (!claimed)
            {
                throw new ProductDraftStateException("This draft has already been turned into a product.");
            }

            BusinessProductDetailResponse product;

            try
            {
                // Deliberately goes through the same service manual creation uses, so
                // the category guard and metadata validation apply identically no
                // matter how a product was authored.
                product = await _dashboardService.CreateProductAsync(businessId, request, cancellationToken);
            }
            catch
            {
                // Otherwise a rejection here - an unusable category, say - would leave
                // the draft Completed with no product and no way to try again.
                await _draftRepository.ReleaseConfirmationClaimAsync(draftId, cancellationToken);
                throw;
            }

            draft.ProductId = product.Id;
            draft.Status = ProductDraftStatus.Completed;
            draft.UpdatedAt = DateTime.UtcNow;

            await _draftRepository.SaveChangesAsync(cancellationToken);

            return product;
        }

        public async Task<ProductDraftResponse> CancelAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            CancellationToken cancellationToken = default)
        {
            var draft = await LoadDraftAsync(businessId, draftId, cancellationToken);

            if (draft.Status == ProductDraftStatus.Completed)
            {
                throw new ProductDraftStateException("This draft has already been turned into a product.");
            }

            draft.Status = ProductDraftStatus.Cancelled;
            draft.UpdatedAt = DateTime.UtcNow;

            await _draftRepository.SaveChangesAsync(cancellationToken);

            return await BuildResponseAsync(draft, [], cancellationToken);
        }

        // ---- one conversation turn ----

        private async Task<ProductDraftResponse> RunTurnAsync(
            ProductDraft draft,
            Guid userId,
            string userMessage,
            string kind,
            CancellationToken cancellationToken)
        {
            var form = await _dashboardService.GetProductFormAsync(draft.BusinessId, cancellationToken);
            var business = await _dashboardRepository.GetBusinessSummaryAsync(draft.BusinessId, cancellationToken);

            ProductDraftState.AppendMessage(draft, "user", userMessage, kind);

            var context = new ProductAiContext
            {
                BusinessName = business?.Name ?? string.Empty,
                Currency = "USD",
                Categories = form.Categories
                    .Select(c => new ProductAiCategory { Id = c.Id, Name = c.Name })
                    .ToList(),
                MetadataFields = form.MetadataFields
                    .Select(f => new ProductAiField
                    {
                        Key = f.Key,
                        Label = f.Label,
                        ValueType = f.ValueType,
                        IsRequired = f.IsRequired,
                        AllowedValues = f.AllowedValues,
                    })
                    .ToList(),
                CurrentDraft = ProductDraftState.ReadDraft(draft),
                // Excludes the message just appended, which is passed separately so
                // the agent can tell "what was said before" from "what to respond to".
                // Every owner turn is followed by exactly one assistant reply, so
                // *2 messages covers VoiceHistoryTurnLimit owner turns.
                History = ProductDraftState.ToPromptHistory(draft, VoiceHistoryTurnLimit * 2 + 1)
                    .SkipLast(1)
                    .ToList(),
                LatestUserMessage = userMessage,
            };

            var scope = new AiInteractionScope(
                draft.BusinessId,
                userId,
                draft.Id,
                _conversationClient.ModelName);

            _aiLogger.LogTurnStarted(scope, kind);

            var stopwatch = Stopwatch.StartNew();

            ProductAiTurnResult result;

            try
            {
                result = await _conversationClient.ContinueConversationAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _aiLogger.LogTurnFailed(scope, stopwatch.ElapsedMilliseconds, ex);

                // The user's message is already persisted, so the conversation is
                // resumable — the draft is not marked Failed for a transient provider
                // error, which would strand a recoverable conversation.
                await _draftRepository.SaveChangesAsync(cancellationToken);

                throw new AiConversationException("The assistant is unavailable right now. Please try again.", ex);
            }

            stopwatch.Stop();

            var decision = result.Decision;

            _aiLogger.LogTurnSucceeded(
                scope,
                decision.Action,
                decision.MissingFields.Count,
                stopwatch.ElapsedMilliseconds,
                result.PromptTokens,
                result.CompletionTokens);

            // Charged after a successful call, never before: a failed provider call
            // already short-circuits above without reaching here, so the owner is
            // never billed for a turn that produced nothing. Authorization was already
            // checked before this request reached the service - this is metering the
            // use that already happened, not gating it, so a false result (credits ran
            // out in the narrow window between the two) is logged rather than turned
            // into an error the owner can't do anything about at this point.
            var creditSpent = await _featureCreditService.TryConsumeAsync(
                draft.BusinessId, FeatureKeys.AiProductGeneration, draft.Id.ToString(), cancellationToken);

            if (!creditSpent)
            {
                _aiLogger.LogValidationRejected(scope, "ai_credits_exhausted");
            }

            await ApplyDecisionAsync(draft, decision, scope, cancellationToken);

            draft.LastMessageAt = DateTime.UtcNow;
            draft.UpdatedAt = DateTime.UtcNow;

            await _draftRepository.SaveChangesAsync(cancellationToken);

            return await BuildResponseAsync(draft, decision.MissingFields, cancellationToken, form);
        }

        private async Task ApplyDecisionAsync(
            ProductDraft draft,
            ProductAiDecision decision,
            AiInteractionScope scope,
            CancellationToken cancellationToken)
        {
            var formData = await _dashboardRepository.GetProductFormDataAsync(
                draft.BusinessId,
                cancellationToken);

            var rejectedValues = new List<string>();

            if (decision.Draft is not null)
            {
                // Categories are validated here because the agent picks one from a
                // list we supplied; anything else is a hallucination and is dropped
                // rather than carried to confirmation where it would fail confusingly.
                if (decision.Draft.CategoryId is { } categoryId)
                {
                    var usable = await _dashboardRepository.CanUseCategoryAsync(
                        draft.BusinessId,
                        categoryId,
                        cancellationToken);

                    if (!usable)
                    {
                        _aiLogger.LogValidationRejected(scope, "category_not_usable");
                        decision.Draft.CategoryId = null;
                    }
                }

                // Metadata values are checked against the business's configured sets
                // for the same reason as the category, and at the same point.
                //
                // The prompt tells the agent to ask rather than substitute, and it
                // usually does - but observed against the live model it sometimes
                // returns a value outside the set anyway. Leaving it in the draft would
                // show the owner "Purple" in the preview and only fail when they press
                // create, so it is removed the moment it is proposed.
                rejectedValues = StripDisallowedMetadata(decision.Draft, formData?.MetadataShape);

                if (rejectedValues.Count > 0)
                {
                    _aiLogger.LogValidationRejected(scope, "metadata_value_not_allowed");
                }

                ProductDraftState.WriteDraft(draft, decision.Draft);
            }

            if (!string.IsNullOrWhiteSpace(decision.Message))
            {
                ProductDraftState.AppendMessage(draft, "assistant", decision.Message, "text");
            }
            else
            {
                // The schema requires a message but does not stop it being empty, and
                // the model occasionally sends nothing. A silent assistant reads as a
                // broken chat, so the turn still gets a reply.
                ProductDraftState.AppendMessage(draft, "assistant", "Got it.", "text");
            }

            // Said explicitly rather than dropped quietly: the owner needs to know the
            // value was not accepted, and which values are.
            if (rejectedValues.Count > 0)
            {
                foreach (var rejected in rejectedValues)
                {
                    ProductDraftState.AppendMessage(draft, "assistant", rejected, "text");
                }
            }

            switch (decision.Action)
            {
                case ProductAiAction.ReadyForReview:
                    // Only a preview state. Creating the product still requires the
                    // owner to confirm.
                    draft.Status = ProductDraftStatus.WaitingForProductApproval;
                    break;

                case ProductAiAction.Cancel:
                    draft.Status = ProductDraftStatus.Cancelled;
                    break;

                case ProductAiAction.RequestInformation:
                    draft.Status = ProductDraftStatus.WaitingForMissingInformation;
                    break;

                default:
                    draft.Status = ProductDraftStatus.CollectingInformation;
                    break;
            }
        }


        [System.Text.RegularExpressions.GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
        private static partial System.Text.RegularExpressions.Regex HexColorPattern();

        /// <summary>
        /// Removes metadata values the business does not permit, returning a message
        /// for each field that lost something.
        ///
        /// Two independent reasons a value gets removed: it's outside a closed
        /// allowedValues set, or - for ColorList, which normally has no allowedValues
        /// at all - it isn't a hex code. Both are checked here, before confirmation,
        /// rather than left to fail at product creation: ProductMetadataBuilder
        /// rejects a non-hex ColorList value with a hard exception, and a colour
        /// name reaching that far would surface as a crash instead of a chat message.
        ///
        /// Removal rather than rejection of the whole turn: the rest of what the owner
        /// said is still good, and losing it because one value was wrong would be worse
        /// than asking again about that one field.
        ///
        /// Internal rather than private: ImageSuggestionService reuses this exact
        /// validation for the photo-analysis flow, which carries the same
        /// hallucination risk against the same business-configured field rules.
        /// </summary>
        internal static List<string> StripDisallowedMetadata(
            ProductAiDraft state,
            JsonDocument? metadataShape)
        {
            if (state.Metadata is null || state.Metadata.Count == 0)
            {
                return [];
            }

            var rules = ProductMetadataBuilder.ReadShape(metadataShape);
            var messages = new List<string>();

            foreach (var key in state.Metadata.Keys.ToList())
            {
                if (!rules.TryGetValue(key, out var rule))
                {
                    continue;
                }

                Func<string, bool>? isAllowed = rule.AllowedValues.Count > 0
                    ? v => rule.AllowedValues.Contains(v, StringComparer.OrdinalIgnoreCase)
                    : rule.ValueType == ProductAttributeValueType.ColorList
                        ? v => HexColorPattern().IsMatch(v)
                        : null;

                if (isAllowed is null)
                {
                    continue;
                }

                var expectation = rule.AllowedValues.Count > 0
                    ? $"Please choose from: {string.Join(", ", rule.AllowedValues)}."
                    : "Colors must be hex codes, like #RRGGBB.";

                var value = state.Metadata[key];

                var offending = value.ValueKind switch
                {
                    JsonValueKind.String => isAllowed(value.GetString()!)
                        ? []
                        : new List<string> { value.GetString()! },
                    JsonValueKind.Array => value.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .Where(v => !isAllowed(v))
                        .ToList(),
                    _ => [],
                };

                if (offending.Count == 0)
                {
                    continue;
                }

                // Keep whatever was valid; only the unsupported entries go.
                if (value.ValueKind == JsonValueKind.Array)
                {
                    var kept = value.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .Where(isAllowed)
                        .ToList();

                    if (kept.Count > 0)
                    {
                        state.Metadata[key] = JsonSerializer.SerializeToDocument(kept).RootElement.Clone();
                    }
                    else
                    {
                        state.Metadata.Remove(key);
                    }
                }
                else
                {
                    state.Metadata.Remove(key);
                }

                messages.Add(
                    $"{string.Join(" and ", offending)} isn't available for {key}. {expectation}");
            }

            return messages;
        }

        // ---- helpers ----

        private async Task<ProductDraft> LoadDraftAsync(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken)
        {
            return await _draftRepository.GetForBusinessAsync(businessId, draftId, cancellationToken)
                ?? throw new ProductDraftNotFoundException();
        }

        private static void EnsureConversationOpen(ProductDraft draft)
        {
            if (draft.Status is ProductDraftStatus.Completed
                or ProductDraftStatus.Cancelled
                or ProductDraftStatus.Failed)
            {
                throw new ProductDraftStateException("This conversation has ended.");
            }

            if (draft.Status == ProductDraftStatus.WaitingForImageApproval)
            {
                throw new ProductDraftStateException("Please accept or reject the updated image first.");
            }
        }

        /// <summary>
        /// What the product still needs before it can be created. Mirrors the fixed
        /// columns on Product; optional metadata is never required, because those
        /// fields are optional by definition.
        /// </summary>
        internal static List<string> FindMissingFields(
            ProductAiDraft? state,
            bool hasImage,
            JsonDocument? metadataShape)
        {
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(state?.Title)) missing.Add("title");
            if (state?.Price is null) missing.Add("price");
            if (state?.CategoryId is null) missing.Add("category");

            // A catalog listing without a picture is not something an owner would
            // want published, so the assistant asks for one before offering to create.
            if (!hasImage) missing.Add("image");

            // Required metadata is per-domain configuration, so it is read rather than
            // hardcoded - a Restaurant business is never asked for a colour.
            foreach (var (key, rule) in ProductMetadataBuilder.ReadShape(metadataShape))
            {
                if (!rule.IsRequired)
                {
                    continue;
                }

                var supplied = state?.Metadata is not null
                    && state.Metadata.TryGetValue(key, out var value)
                    && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                    // An empty list satisfies nothing; "sizes: []" is still no sizes.
                    && !(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0)
                    && !(value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString()));

                if (!supplied)
                {
                    missing.Add($"metadata.{key}");
                }
            }

            return missing;
        }

        private static string BuildGreeting(ProductFormResponse form)
        {
            var extras = form.MetadataFields.Count > 0
                ? $" I'll also ask about {string.Join(", ", form.MetadataFields.Select(f => f.Label.ToLowerInvariant()))} if they apply."
                : string.Empty;

            return "Hi! I can set up a product for you. Tell me what it is, "
                + $"what it costs, and which category it belongs to.{extras} "
                + "You can type or record a voice message.";
        }

        /// <param name="knownForm">
        /// Passed in by callers that already loaded it, so a turn doesn't fetch the
        /// same categories and field configuration twice.
        /// </param>
        private async Task<ProductDraftResponse> BuildResponseAsync(
            ProductDraft draft,
            List<string> agentMissingFields,
            CancellationToken cancellationToken,
            ProductFormResponse? knownForm = null)
        {
            var state = ProductDraftState.ReadDraft(draft);

            string? categoryName = null;

            if (state?.CategoryId is { } categoryId)
            {
                var form = knownForm
                    ?? await _dashboardService.GetProductFormAsync(draft.BusinessId, cancellationToken);

                categoryName = form.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
            }

            var formData = await _dashboardRepository.GetProductFormDataAsync(draft.BusinessId, cancellationToken);

            var backendMissing = FindMissingFields(
                state,
                draft.OriginalImageUrl is not null,
                formData?.MetadataShape);

            return new ProductDraftResponse
            {
                Id = draft.Id,
                Status = draft.Status.ToString(),
                Messages = ProductDraftState.ReadMessages(draft)
                    .Select(m => new ProductDraftMessageResponse
                    {
                        Role = m.Role,
                        Text = m.Text,
                        Kind = m.Kind,
                        At = m.At,
                    })
                    .ToList(),
                Draft = state is null ? null : new ProductDraftProductResponse
                {
                    Title = state.Title,
                    Description = state.Description,
                    Price = state.Price,
                    CompareAtPrice = state.CompareAtPrice,
                    CategoryId = state.CategoryId,
                    CategoryName = categoryName,
                    Sku = state.Sku,
                    StockQuantity = state.StockQuantity,
                    Tags = state.Tags,
                    SaleEndsAt = state.SaleEndsAt,
                    Metadata = state.Metadata is null
                        ? null
                        : JsonSerializer.SerializeToDocument(state.Metadata),
                },
                // Always ours, never the agent's. Preferring the agent's list when it
                // offered one made this field inconsistent: it named things differently
                // ("colors" against our "metadata.colors") and could omit a field it
                // had overlooked, so a client could not rely on either the naming or
                // the completeness. The agent's opinion is still visible in its message.
                MissingFields = backendMissing,
                // Stored as object keys, like every other product image.
                OriginalImageUrl = _productImageUrlResolver.ToPublicUrl(draft.OriginalImageUrl),
                ProcessedImageUrl = _productImageUrlResolver.ToPublicUrl(draft.ProcessedImageUrl),
                ImageModificationPrompt = draft.ImageModificationPrompt,
                CanConfirm = backendMissing.Count == 0
                    && draft.Status is not (ProductDraftStatus.Completed
                        or ProductDraftStatus.Cancelled
                        or ProductDraftStatus.Failed
                        or ProductDraftStatus.WaitingForImageApproval),
                ProductId = draft.ProductId,
            };
        }
    }
}
