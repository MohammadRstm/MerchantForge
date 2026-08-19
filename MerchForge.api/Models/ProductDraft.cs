using System.Text.Json;
using MerchForge.api.Enums;

namespace MerchForge.api.Models;

/// <summary>
/// Persistent state of an unfinished product-creation conversation.
///
/// Deliberately persistent rather than in-memory: the owner can close the modal,
/// lose connection or come back tomorrow, and the conversation has to be
/// recoverable. Everything needed to rebuild the chat and the in-progress product
/// lives here.
/// </summary>
public class ProductDraft
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>
    /// The owner who started the conversation. Recorded for audit and logging;
    /// authorization is by business membership, not by this field, so a co-owner
    /// can still resume a draft.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    public ProductDraftStatus Status { get; set; } = ProductDraftStatus.CollectingInformation;

    /// <summary>
    /// The structured product being assembled, in the same shape the AI reads and
    /// returns: { title, description, price, categoryId, metadata }. Null until the
    /// agent has extracted anything.
    ///
    /// This is the state the agent edits. Feeding it back on every turn is what lets
    /// "actually make it $29" change a price instead of starting a new product.
    /// </summary>
    public JsonDocument? Draft { get; set; }

    /// <summary>
    /// Conversation history, intentionally shaped rather than dumped provider
    /// objects: { "messages": [ { role, text, kind, at } ] }. Storing our own shape
    /// keeps the record readable and survives a provider swap.
    /// </summary>
    public JsonDocument? Messages { get; set; }

    /// <summary>Image as uploaded by the owner.</summary>
    public string? OriginalImageUrl { get; set; }

    /// <summary>Result of an image modification, pending or approved.</summary>
    public string? ProcessedImageUrl { get; set; }

    /// <summary>
    /// The modification the agent understood the owner to be asking for
    /// ("make the background neutral"). Kept so a failed or rejected edit can be
    /// retried or shown back to the owner.
    /// </summary>
    public string? ImageModificationPrompt { get; set; }

    /// <summary>
    /// Set once the owner confirms, linking the draft to what it produced. Also the
    /// guard against a double confirmation creating two products.
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Ingestion channel, for drafts that will later arrive from Telegram/WhatsApp
    /// rather than the dashboard. Null for dashboard-initiated conversations, which
    /// have no external conversation to correlate with.
    /// </summary>
    public string? Provider { get; set; }

    public string? ConversationId { get; set; }

    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation

    public Business Business { get; set; } = null!;

    public Product? Product { get; set; }
}
