namespace MerchForge.api.Configurations;

public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>
    /// Provider API key. Supplied through user secrets or the environment only —
    /// never appsettings, and never logged.
    /// </summary>
    public string? ApiKey { get; set; }

    public string ConversationModel { get; set; } = "gpt-4o-mini";

    public string TranscriptionModel { get; set; } = "whisper-1";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// Whether a provider is configured at all. When false the app registers
    /// unavailable implementations that fail cleanly, so the rest of MerchForge still
    /// starts and every non-AI feature keeps working.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
