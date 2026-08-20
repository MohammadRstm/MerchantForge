using System.Text.Json;
using System.Text.Json.Serialization;
using MerchForge.api.Models;
using MerchForge.api.Services.AI.Contracts;

namespace MerchForge.api.Services.ProductAi;

/// <summary>
/// Reads and writes the two json columns on ProductDraft.
///
/// Kept separate from the orchestration so the stored shape is defined in exactly
/// one place: the draft's product state and its message history are persisted data
/// that has to survive restarts and provider changes, not incidental serialization.
/// </summary>
public static class ProductDraftState
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ---- product state ----

    public static ProductAiDraft? ReadDraft(ProductDraft draft)
    {
        if (draft.Draft is null)
        {
            return null;
        }

        try
        {
            return draft.Draft.Deserialize<ProductAiDraft>(SerializerOptions);
        }
        catch (JsonException)
        {
            // A draft we can't read is treated as empty rather than crashing the
            // conversation: the owner can continue, and the agent rebuilds state from
            // the message history that is still intact.
            return null;
        }
    }

    public static void WriteDraft(ProductDraft draft, ProductAiDraft? state)
    {
        draft.Draft = state is null
            ? null
            : JsonSerializer.SerializeToDocument(state, SerializerOptions);
    }

    // ---- conversation history ----

    public static List<StoredMessage> ReadMessages(ProductDraft draft)
    {
        if (draft.Messages is null)
        {
            return [];
        }

        try
        {
            return draft.Messages.Deserialize<StoredMessageLog>(SerializerOptions)?.Messages ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static void AppendMessage(ProductDraft draft, string role, string text, string kind)
    {
        var messages = ReadMessages(draft);

        messages.Add(new StoredMessage
        {
            Role = role,
            Text = text,
            Kind = kind,
            At = DateTime.UtcNow,
        });

        draft.Messages = JsonSerializer.SerializeToDocument(
            new StoredMessageLog { Messages = messages },
            SerializerOptions);
    }

    /// <summary>
    /// The tail of the conversation, oldest first, for prompting.
    ///
    /// Capped deliberately: sending an unbounded history grows cost and latency on
    /// every turn for context the agent rarely needs, since the structured draft
    /// already carries the accumulated state. Recent turns are what disambiguate a
    /// reply like "the second one".
    /// </summary>
    public static List<ProductAiMessage> ToPromptHistory(ProductDraft draft, int maxMessages)
    {
        return ReadMessages(draft)
            .TakeLast(maxMessages)
            .Select(m => new ProductAiMessage { Role = m.Role, Text = m.Text })
            .ToList();
    }

    /// <summary>An object with a "messages" array, so the format can gain siblings later.</summary>
    public class StoredMessageLog
    {
        public List<StoredMessage> Messages { get; set; } = [];
    }

    public class StoredMessage
    {
        public string Role { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string Kind { get; set; } = "text";

        public DateTime At { get; set; }
    }
}
