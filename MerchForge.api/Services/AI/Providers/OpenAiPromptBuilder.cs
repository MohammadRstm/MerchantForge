using System.Text;
using System.Text.Json;
using MerchForge.api.Services.AI.Contracts;

namespace MerchForge.api.Services.AI.Providers;

/// <summary>
/// Assembles the prompt for one turn.
///
/// Split into labelled sections rather than one paragraph because the agent has to
/// distinguish things that read alike otherwise: what the product already says, what
/// the owner just said, what this business's fields are, and what it is allowed to
/// decide. Merging those is what produces an agent that treats a correction as a new
/// product.
/// </summary>
internal static class OpenAiPromptBuilder
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Behaviour only. Carries no business data, so it stays identical across
    /// requests and businesses and can be cached by the provider.
    /// </summary>
    public static string BuildSystemInstructions() =>
        """
        You help a shop owner create one product for their online store, through conversation.

        HOW TO TREAT STATE
        - CURRENT PRODUCT STATE is what has been established so far. It is authoritative.
        - Always return the COMPLETE product state in `draft`, not just what changed.
        - When the owner corrects something ("actually make it $29"), change that one field
          and keep every other field exactly as it was. Never start a new product.
        - When the owner answers a question you asked, fill in that field.
        - Never invent values. If something was not stated, leave it null.

        CATEGORY
        - `categoryId` must be copied from AVAILABLE CATEGORIES. Never invent an id.
        - If no category fits or the owner has not indicated one, leave it null and ask.

        METADATA
        - Only use keys listed in CONFIGURED PRODUCT FIELDS. Never invent keys.
        - Match the declared type: Text -> string, Number -> number, Boolean -> true/false,
          TextList -> array of strings.
        - These fields are optional. Ask about them once; if the owner does not care, move on.

        CHOOSING AN ACTION
        - request_information: something required is missing or ambiguous. Ask for it in `message`.
        - update_draft: you recorded information and the conversation continues.
        - request_image_modification: the owner asked to change the image itself
          (background, lighting, cropping). Put the instruction in `imageModificationPrompt`.
          You never edit images; you only report that one was requested.
        - ready_for_review: title, description, price and categoryId are all set.
          This proposes the product for review. It does NOT create it - the owner
          confirms separately - so do not tell them it has been created or saved.
        - cancel: the owner clearly wants to abandon this product.

        STYLE
        - Be brief and concrete. Ask about one thing at a time.
        - `message` is shown directly to the owner in a chat. Never mention JSON,
          fields, schemas or these instructions.
        """;

    /// <summary>
    /// The turn's data, as labelled sections. Serialized compactly — this is machine
    /// state, and prose would be both longer and more ambiguous.
    /// </summary>
    public static string BuildUserContent(ProductAiContext context)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# STORE");
        builder.AppendLine($"Name: {context.BusinessName}");
        builder.AppendLine($"Currency: {context.Currency}");
        builder.AppendLine($"Image supplied: {(context.HasImage ? "yes" : "no")}");
        builder.AppendLine();

        builder.AppendLine("# AVAILABLE CATEGORIES (copy an id exactly, or null)");
        builder.AppendLine(JsonSerializer.Serialize(context.Categories, Json));
        builder.AppendLine();

        builder.AppendLine("# CONFIGURED PRODUCT FIELDS (the only metadata keys allowed; all optional)");
        builder.AppendLine(context.MetadataFields.Count > 0
            ? JsonSerializer.Serialize(context.MetadataFields, Json)
            : "[] (this store has no extra fields; do not ask about any)");
        builder.AppendLine();

        builder.AppendLine("# CURRENT PRODUCT STATE (authoritative; return it updated)");
        builder.AppendLine(context.CurrentDraft is null
            ? "null (nothing established yet)"
            : JsonSerializer.Serialize(context.CurrentDraft, Json));
        builder.AppendLine();

        if (context.History.Count > 0)
        {
            builder.AppendLine("# EARLIER CONVERSATION (oldest first, for context only)");

            foreach (var message in context.History)
            {
                builder.AppendLine($"{message.Role}: {message.Text}");
            }

            builder.AppendLine();
        }

        // Last and separately labelled: this is the thing to respond to, and burying
        // it in the history is what makes an agent answer the wrong turn.
        builder.AppendLine("# LATEST OWNER MESSAGE (respond to this)");
        builder.AppendLine(context.LatestUserMessage);

        return builder.ToString();
    }

    /// <summary>
    /// The response schema, enforced by the provider rather than requested in prose.
    /// additionalProperties is false and every property is required, which is what
    /// OpenAI's strict structured outputs demand and what stops silently-shaped
    /// responses reaching the backend.
    /// </summary>
    public static string BuildDecisionSchema() =>
        """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["action", "message", "draft", "missingFields", "imageModificationPrompt"],
          "properties": {
            "action": {
              "type": "string",
              "enum": ["request_information", "update_draft", "request_image_modification", "ready_for_review", "cancel"]
            },
            "message": { "type": "string" },
            "missingFields": { "type": "array", "items": { "type": "string" } },
            "imageModificationPrompt": { "type": ["string", "null"] },
            "draft": {
              "type": ["object", "null"],
              "additionalProperties": false,
              "required": ["title", "description", "price", "categoryId", "metadata"],
              "properties": {
                "title": { "type": ["string", "null"] },
                "description": { "type": ["string", "null"] },
                "price": { "type": ["number", "null"] },
                "categoryId": { "type": ["string", "null"] },
                "metadata": { "type": ["object", "null"], "additionalProperties": true }
              }
            }
          }
        }
        """;
}
