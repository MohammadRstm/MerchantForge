namespace MerchForge.api.Services.AI.Contracts;

/// <summary>
/// Everything the agent needs for one turn, and nothing else.
///
/// Deliberately not EF entities: the provider should never receive owner emails,
/// subscription state, internal ids it has no use for, or audit columns. Each part
/// is a separate property rather than one blob so the prompt can present them as
/// distinct sections — the agent has to be able to tell existing product state apart
/// from the latest message apart from the business's field configuration.
/// </summary>
public class ProductAiContext
{
    /// <summary>Store name, used only so the agent can address the owner naturally.</summary>
    public string BusinessName { get; set; } = string.Empty;

    public string Currency { get; set; } = "USD";

    /// <summary>Categories the product may be assigned to. The agent must pick one of these.</summary>
    public List<ProductAiCategory> Categories { get; set; } = [];

    /// <summary>
    /// The optional metadata fields this business configured. Different businesses
    /// have entirely different fields, so this is always supplied rather than assumed.
    /// </summary>
    public List<ProductAiField> MetadataFields { get; set; } = [];

    /// <summary>
    /// The product built so far. Null on the first turn. The agent is instructed to
    /// return this updated, not to rebuild it.
    /// </summary>
    public ProductAiDraft? CurrentDraft { get; set; }

    /// <summary>Prior turns, oldest first.</summary>
    public List<ProductAiMessage> History { get; set; } = [];

    /// <summary>The message being responded to, already transcribed if it was voice.</summary>
    public string LatestUserMessage { get; set; } = string.Empty;
}

public class ProductAiCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>One configurable metadata field, described so the agent knows what shape a value must take.</summary>
public class ProductAiField
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>Text | Number | Boolean | TextList.</summary>
    public string ValueType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    /// <summary>
    /// Permitted values, or empty for free-form. Given to the agent so it can ask the
    /// owner about an unsupported value rather than guessing a substitute.
    /// </summary>
    public List<string> AllowedValues { get; set; } = [];
}

public class ProductAiMessage
{
    /// <summary>"user" or "assistant".</summary>
    public string Role { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
