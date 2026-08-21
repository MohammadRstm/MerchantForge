using MerchForge.api.Exceptions.AI;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;

namespace MerchForge.api.Services.AI.Providers;

/// <summary>
/// Registered when no image-editing provider is configured. Fails loudly on use
/// rather than silently returning the original image, which would look like a
/// successful edit that did nothing.
/// </summary>
public class UnavailableProductImageEditingClient : IProductImageEditingClient
{
    public string ModelName => "unconfigured";

    public Task<ImageEditResult> EditAsync(
        List<ImageEditInput> images,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        throw new ImageEditingException(
            "AI image editing isn't configured on this server.");
    }
}
