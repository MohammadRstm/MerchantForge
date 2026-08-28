namespace MerchForge.api.Services.Common;

/// <summary>
/// Shared scheme allowlist for any URL a business owner can submit that later gets
/// interpolated into an &lt;a href&gt; by template code this platform doesn't control
/// (social links, WhatsApp-adjacent Url/Link customization fields). Used by both
/// WebsiteCustomizationValuesBuilder and SaveWebsiteCustomizationDraftRequestValidator
/// so the two never drift apart on what counts as "safe".
/// </summary>
public static class SafeUrlValidator
{
    private static readonly HashSet<string> AllowedSchemes =
        new(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto", "tel" };

    public static bool IsSafe(string? url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            && AllowedSchemes.Contains(parsed.Scheme);
    }
}
