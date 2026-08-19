using System.Text.Json;

namespace MerchForge.api.Services.AI.Contracts;

/// <summary>
/// What the agent decided this turn. Strongly typed on purpose: the backend never
/// parses free-form prose to work out what happened, it reads an action off a closed
/// enum. Anything the provider returns that doesn't fit this shape is a failure, not
/// something to interpret.
/// </summary>
public class ProductAiDecision
{
    public ProductAiAction Action { get; set; }

    /// <summary>What to show the owner in the chat. Always present.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The full product state after this turn, not a delta. The agent receives the
    /// current draft and returns it updated, so a correction ("actually make it $29")
    /// arrives as the same draft with one field changed rather than as a new product.
    /// </summary>
    public ProductAiDraft? Draft { get; set; }

    /// <summary>
    /// Fields the agent still needs, for the UI to hint at. Advisory only — whether
    /// the product is actually complete is decided by backend validation, not by
    /// this list being empty.
    /// </summary>
    public List<string> MissingFields { get; set; } = [];

    /// <summary>
    /// Set when Action is RequestImageModification. The instruction is passed to the
    /// image service; the agent never edits an image itself.
    /// </summary>
    public string? ImageModificationPrompt { get; set; }
}

public enum ProductAiAction
{
    /// <summary>Something is missing or ambiguous; Message asks for it.</summary>
    RequestInformation,

    /// <summary>Information was recorded; the conversation continues.</summary>
    UpdateDraft,

    /// <summary>The owner asked for an image change; see ImageModificationPrompt.</summary>
    RequestImageModification,

    /// <summary>The agent believes the product is complete and should be previewed.</summary>
    ReadyForReview,

    /// <summary>The owner asked to abandon the draft.</summary>
    Cancel
}

/// <summary>
/// The product state the agent reads and writes. Mirrors the fixed product fields
/// plus this business's configured metadata.
/// </summary>
public class ProductAiDraft
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Values for the business's configured fields, keyed by field key. Raw JSON so
    /// each value keeps the type its field declares — a TextList stays an array
    /// rather than being flattened to a string on the way through.
    /// </summary>
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}
