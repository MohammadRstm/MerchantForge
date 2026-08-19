using System.Text;

namespace MerchForge.api.Services.Common;

/// <summary>
/// Turns a display name into a URL/id-safe slug: lowercase ASCII letters, digits and
/// single hyphens, no leading/trailing/repeated hyphens. "Vintage  Wear!" -> "vintage-wear".
/// </summary>
public static class Slug
{
    public static string From(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lastWasHyphen = false;

        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        if (lastWasHyphen && builder.Length > 0)
        {
            builder.Length--;
        }

        return builder.ToString();
    }
}
