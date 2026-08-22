using MerchForge.api.Services.AI.Contracts;

namespace MerchForge.api.Services.AI.Interfaces;

/// <summary>
/// One call to an image-editing model: N input images plus a text instruction in,
/// one edited image out. A separate boundary from IProductAiConversationClient
/// because this is a different capability with a different provider surface and no
/// conversational state - each call is independent.
/// </summary>
public interface IProductImageEditingClient
{
    string ModelName { get; }

    Task<ImageEditResult> EditAsync(
        List<ImageEditInput> images,
        string prompt,
        CancellationToken cancellationToken = default);
}
