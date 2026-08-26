using MerchForge.api.Exceptions.AI;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;

namespace MerchForge.api.Services.AI.Providers;

/// <summary>
/// Registered when no image-editing provider is configured. Fails loudly on use
/// rather than silently returning an empty draft, which would look like the photo
/// genuinely had nothing extractable from it.
/// </summary>
public class UnavailableProductImageSuggestionClient : IProductImageSuggestionClient
{
    public string ModelName => "unconfigured";

    public Task<ProductAiDraft> SuggestAsync(
        ImageEditInput image,
        ProductAiContext context,
        CancellationToken cancellationToken = default)
    {
        throw new ImageEditingException(
            "AI image analysis isn't configured on this server.");
    }
}
