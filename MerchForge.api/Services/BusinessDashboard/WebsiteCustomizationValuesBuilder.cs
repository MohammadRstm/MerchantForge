using System.Text.Json;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Models;

namespace MerchForge.api.Services.BusinessDashboard;

/// <summary>
/// Turns the template-field values a business owner submitted into the JSON object
/// stored per-template inside Business.WebsiteCustomizationValues (or, before publish,
/// BusinessWebsiteDraft.TemplateFieldsDraft), validated against that template's
/// customizable-component catalogue. Mirrors ProductMetadataBuilder's shape closely
/// (same "unknown key is rejected, not silently discarded" reasoning at save time) but
/// also exposes DropUnknownKeys for the different reasoning publish-time needs: a key
/// that was valid when saved but was retired by a SuperAdmin afterward isn't something
/// the owner caused or can fix, so publish drops it instead of hard-failing the whole
/// publish.
/// </summary>
public static partial class WebsiteCustomizationValuesBuilder
{
    /// <summary>One slot's constraints, as currently registered in the catalogue.</summary>
    public readonly record struct FieldRule(
        WebsiteCustomizableValueType ValueType,
        bool IsRequired,
        IReadOnlyList<string> AllowedValues);

    public static Dictionary<string, FieldRule> BuildRules(
        IEnumerable<WebsiteTemplateCustomizableComponent> components)
    {
        var rules = new Dictionary<string, FieldRule>(StringComparer.Ordinal);

        foreach (var component in components.Where(c => c.IsActive))
        {
            var allowedValues = ReadAllowedValues(component.AllowedValues);
            rules[component.Key] = new FieldRule(component.ValueType, component.IsRequired, allowedValues);
        }

        return rules;
    }

    /// <summary>
    /// Every field is optional at the builder level (required-ness is enforced by the
    /// caller against the whole submission, same as a normal form) — an unset field is
    /// simply absent from the result. What's rejected is a key the template doesn't
    /// declare, or a value of the wrong shape for its declared type, since either would
    /// produce a value no template's code can render.
    /// </summary>
    public static JsonDocument? Build(
        Dictionary<string, FieldRule> allowed,
        Dictionary<string, JsonElement>? submitted)
    {
        if (submitted is null || submitted.Count == 0)
        {
            return null;
        }

        var unknown = submitted.Keys
            .Where(k => !allowed.ContainsKey(k))
            .ToList();

        if (unknown.Count > 0)
        {
            throw new InvalidWebsiteCustomizationValueException(
                $"These fields aren't part of this template: {string.Join(", ", unknown)}.");
        }

        var result = new Dictionary<string, object?>();

        foreach (var (key, rule) in allowed)
        {
            if (!submitted.TryGetValue(key, out var value))
            {
                continue;
            }

            var coerced = Coerce(key, rule.ValueType, value);

            EnsureAllowed(key, rule, coerced);

            if (coerced is not null)
            {
                result[key] = coerced;
            }
        }

        return result.Count == 0
            ? null
            : JsonSerializer.SerializeToDocument(result);
    }

    /// <summary>
    /// Publish-time re-validation: a catalogue key that was valid when the draft was
    /// saved may have been retired since. Filters those out instead of throwing —
    /// the owner didn't cause this and can't fix a field that no longer exists.
    /// </summary>
    public static (JsonDocument? Result, List<string> DroppedKeys) DropUnknownKeys(
        JsonDocument? values,
        Dictionary<string, FieldRule> allowed)
    {
        if (values is null || values.RootElement.ValueKind != JsonValueKind.Object)
        {
            return (null, []);
        }

        var dropped = new List<string>();
        var result = new Dictionary<string, JsonElement>();

        foreach (var property in values.RootElement.EnumerateObject())
        {
            if (allowed.ContainsKey(property.Name))
            {
                result[property.Name] = property.Value.Clone();
            }
            else
            {
                dropped.Add(property.Name);
            }
        }

        var document = result.Count == 0
            ? null
            : JsonSerializer.SerializeToDocument(result);

        return (document, dropped);
    }

    private static List<string> ReadAllowedValues(JsonDocument? allowedValues)
    {
        if (allowedValues is null || allowedValues.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return allowedValues.RootElement
            .EnumerateArray()
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString()!)
            .ToList();
    }

    private static void EnsureAllowed(string key, FieldRule rule, object? coerced)
    {
        if (rule.AllowedValues.Count == 0 || coerced is null)
        {
            return;
        }

        if (coerced is string single && !rule.AllowedValues.Contains(single, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidWebsiteCustomizationValueException(
                $"'{single}' isn't an accepted value for '{key}'. Allowed: {string.Join(", ", rule.AllowedValues)}.");
        }
    }

    private static object? Coerce(string key, WebsiteCustomizableValueType type, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        switch (type)
        {
            case WebsiteCustomizableValueType.Text:
            case WebsiteCustomizableValueType.Textarea:
            case WebsiteCustomizableValueType.Image:
            case WebsiteCustomizableValueType.Select:
                {
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        throw Mismatch(key, "text");
                    }

                    var text = value.GetString()?.Trim();
                    return string.IsNullOrEmpty(text) ? null : text;
                }

            case WebsiteCustomizableValueType.Color:
                {
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        throw Mismatch(key, "a hex color");
                    }

                    var text = value.GetString()?.Trim();

                    if (string.IsNullOrEmpty(text))
                    {
                        return null;
                    }

                    if (!HexColor().IsMatch(text))
                    {
                        throw new InvalidWebsiteCustomizationValueException(
                            $"'{text}' isn't a valid hex color for '{key}'. Expected a format like #RRGGBB.");
                    }

                    return text.ToUpperInvariant();
                }

            case WebsiteCustomizableValueType.Url:
                {
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        throw Mismatch(key, "a url");
                    }

                    var text = value.GetString()?.Trim();
                    return string.IsNullOrEmpty(text) ? null : EnsureSafeUrl(key, text);
                }

            case WebsiteCustomizableValueType.Boolean:
                {
                    if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    {
                        throw Mismatch(key, "true or false");
                    }

                    return value.GetBoolean();
                }

            case WebsiteCustomizableValueType.Number:
                {
                    if (value.ValueKind != JsonValueKind.Number)
                    {
                        throw Mismatch(key, "a number");
                    }

                    return value.GetDecimal();
                }

            case WebsiteCustomizableValueType.Link:
                {
                    if (value.ValueKind != JsonValueKind.Object
                        || !value.TryGetProperty("text", out var textElement)
                        || !value.TryGetProperty("url", out var urlElement)
                        || textElement.ValueKind != JsonValueKind.String
                        || urlElement.ValueKind != JsonValueKind.String)
                    {
                        throw Mismatch(key, "an object with 'text' and 'url'");
                    }

                    var linkText = textElement.GetString()?.Trim();
                    var linkUrl = urlElement.GetString()?.Trim();

                    if (string.IsNullOrEmpty(linkText) || string.IsNullOrEmpty(linkUrl))
                    {
                        return null;
                    }

                    return new Dictionary<string, string>
                    {
                        ["text"] = linkText,
                        ["url"] = EnsureSafeUrl(key, linkUrl),
                    };
                }

            default:
                return null;
        }
    }

    private static readonly HashSet<string> AllowedUrlSchemes =
        new(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto", "tel" };

    /// <summary>
    /// Both Url and Link values eventually get interpolated into an &lt;a href&gt; by
    /// template code this platform doesn't control, so a scheme allowlist is enforced
    /// here rather than trusting every current and future template to sanitize on
    /// render.
    /// </summary>
    private static string EnsureSafeUrl(string key, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || !AllowedUrlSchemes.Contains(parsed.Scheme))
        {
            throw new InvalidWebsiteCustomizationValueException(
                $"'{url}' isn't a valid link for '{key}'. Only http, https, mailto, and tel links are allowed.");
        }

        return url;
    }

    [System.Text.RegularExpressions.GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial System.Text.RegularExpressions.Regex HexColor();

    private static InvalidWebsiteCustomizationValueException Mismatch(string key, string expected) =>
        new($"'{key}' must be {expected}.");
}
