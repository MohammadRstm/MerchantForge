namespace MerchForge.api.Configurations;

public class GeminiOptions
{
    public const string SectionName = "GeminiFlash";

    /// <summary>
    /// Provider API key. Supplied through user secrets or the environment only —
    /// never appsettings, and never logged.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Nano Banana 2 Lite - the cheapest and fastest model in the Gemini 3.1 image
    /// family that still accepts multiple input images plus a text instruction in one
    /// call, which is all this feature needs. Swap this if quality ever needs to beat
    /// cost (gemini-3.1-flash-image or gemini-3-pro-image are drop-in alternatives on
    /// the same API).
    /// </summary>
    public string ImageEditingModel { get; set; } = "gemini-3.1-flash-lite-image";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";

    /// <summary>
    /// Whether a provider is configured at all. When false the app registers an
    /// unavailable implementation that fails cleanly, so the rest of MerchForge still
    /// starts and every non-AI feature keeps working.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
