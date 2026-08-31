using System.Text.Json;
using MerchForge.api.DTOs.ProductAi;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.ImageSuggestion.Interfaces;
using MerchForge.api.Services.ProductAi;
using MerchForge.api.Services.Subscription.interfaces;
using Microsoft.Extensions.Logging;

namespace MerchForge.api.Services.ImageSuggestion;

/// <summary>
/// AI-assisted "read this photo" — the third independent AI image feature,
/// metered from the same ai.image_editing credit pool as the other two but
/// producing a structured product draft instead of pixels. Stateless: unlike the
/// voice "fill with AI" flow, no ProductDraft row is created here — this reuses
/// that flow's field schema and output shape, not its persistence.
/// </summary>
public class ImageSuggestionService : IImageSuggestionService
{
    private readonly IProductImageService _imageService;
    private readonly IProductImageSuggestionClient _suggestionClient;
    private readonly IBusinessDashboardService _dashboardService;
    private readonly IBusinessDashboardRepository _dashboardRepository;
    private readonly IFeatureCreditService _featureCreditService;
    private readonly ILogger<ImageSuggestionService> _logger;

    public ImageSuggestionService(
        IProductImageService imageService,
        IProductImageSuggestionClient suggestionClient,
        IBusinessDashboardService dashboardService,
        IBusinessDashboardRepository dashboardRepository,
        IFeatureCreditService featureCreditService,
        ILogger<ImageSuggestionService> logger)
    {
        _imageService = imageService;
        _suggestionClient = suggestionClient;
        _dashboardService = dashboardService;
        _dashboardRepository = dashboardRepository;
        _featureCreditService = featureCreditService;
        _logger = logger;
    }

    public async Task<ProductDraftProductResponse> SuggestAsync(
        Guid businessId,
        Guid userId,
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new InvalidImageEditRequestException("Select an image to analyze.");
        }

        // Read back rather than trusted as-sent, same reasoning as image editing:
        // this confirms the url actually belongs to this business before anything
        // is sent to a third party.
        var (bytes, contentType) = await _imageService.ReadAsync(businessId, imageUrl, cancellationToken);

        var form = await _dashboardService.GetProductFormAsync(businessId, cancellationToken);
        var business = await _dashboardRepository.GetBusinessSummaryAsync(businessId, cancellationToken);

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
        };

        ProductAiDraft draft;

        try
        {
            draft = await _suggestionClient.SuggestAsync(
                new ImageEditInput(bytes, contentType), context, cancellationToken);
        }
        catch (Exception ex)
        {
            // Normalizes every failure - a provider timeout, a dropped connection, a
            // bad response - to one clean, actionable message, the same way
            // ProductAiService does for the conversation flow. Without this, a
            // network-level failure (nothing the GeminiImageSuggestionClient's own
            // status-code check ever sees) would reach the client as a generic 500
            // instead of "try again".
            throw new ImageEditingException("The image suggestion provider is unavailable right now. Please try again.", ex);
        }

        // Charged after a successful call, never before - same reasoning as every
        // other AI call in this codebase: a failed provider call already threw
        // above, so the owner is never billed for an analysis that produced
        // nothing.
        var creditSpent = await _featureCreditService.TryConsumeAsync(
            businessId, FeatureKeys.AiImageEditing, Guid.NewGuid().ToString(), cancellationToken);

        if (!creditSpent)
        {
            _logger.LogWarning(
                "Image suggestion for business {BusinessId} completed with no credit available to spend.",
                businessId);
        }

        // The model only ever saw the id/name lists we gave it, but it can still
        // hallucinate a category outside them or a metadata value outside an
        // allowed set - the exact same risk the voice flow's agent carries, so the
        // exact same validation applies before this reaches the owner.
        string? categoryName = null;

        if (draft.CategoryId is { } categoryId)
        {
            var usable = await _dashboardRepository.CanUseCategoryAsync(businessId, categoryId, cancellationToken);

            if (usable)
            {
                categoryName = form.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
            }
            else
            {
                draft.CategoryId = null;
            }
        }

        var formData = await _dashboardRepository.GetProductFormDataAsync(businessId, cancellationToken);
        ProductAiService.StripDisallowedMetadata(draft, formData?.MetadataShape);

        // The model sometimes includes a key with an explicit JSON null instead of
        // omitting it entirely - both mean the same thing ("couldn't determine
        // this from the photo"), so both are treated as not-filled rather than
        // letting a literal null read as a real, applicable value.
        if (draft.Metadata is not null)
        {
            foreach (var key in draft.Metadata.Keys.ToList())
            {
                if (draft.Metadata[key].ValueKind == JsonValueKind.Null)
                {
                    draft.Metadata.Remove(key);
                }
            }

            if (draft.Metadata.Count == 0)
            {
                draft.Metadata = null;
            }
        }

        return new ProductDraftProductResponse
        {
            Title = draft.Title,
            Description = draft.Description,
            Price = draft.Price,
            CompareAtPrice = draft.CompareAtPrice,
            CategoryId = draft.CategoryId,
            CategoryName = categoryName,
            Sku = draft.Sku,
            StockQuantity = draft.StockQuantity,
            Tags = draft.Tags,
            SaleEndsAt = draft.SaleEndsAt,
            Metadata = draft.Metadata is null
                ? null
                : JsonSerializer.SerializeToDocument(draft.Metadata),
        };
    }
}
