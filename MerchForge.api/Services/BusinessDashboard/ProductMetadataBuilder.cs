using System.Text.Json;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.BusinessDashboard;

namespace MerchForge.api.Services.BusinessDashboard;

/// <summary>
/// Turns the metadata a merchant submitted into the JSON document stored on a
/// product, validated against that business's metadata shape.
///
/// Every field is optional — a business opting into "colors" doesn't mean every
/// product has colors — so omitted and blank values are dropped rather than rejected.
/// What is rejected is a key the business never opted into, or a value of the wrong
/// type, since either would produce metadata no product form can render back.
/// </summary>
public static class ProductMetadataBuilder
{
    public static JsonDocument? Build(
        JsonDocument? metadataShape,
        Dictionary<string, JsonElement>? submitted)
    {
        if (submitted is null || submitted.Count == 0)
        {
            return null;
        }

        var allowed = ReadShape(metadataShape);

        var unknown = submitted.Keys
            .Where(k => !allowed.ContainsKey(k))
            .ToList();

        if (unknown.Count > 0)
        {
            throw new InvalidProductMetadataException(
                $"These fields aren't enabled for this business: {string.Join(", ", unknown)}.");
        }

        var result = new Dictionary<string, object?>();

        foreach (var (key, definedType) in allowed)
        {
            if (!submitted.TryGetValue(key, out var value))
            {
                continue;
            }

            var coerced = Coerce(key, definedType, value);

            // Null means "left blank" — the field simply doesn't appear on this
            // product, which is different from it being invalid.
            if (coerced is not null)
            {
                result[key] = coerced;
            }
        }

        return result.Count == 0
            ? null
            : JsonSerializer.SerializeToDocument(result);
    }

    private static Dictionary<string, ProductAttributeValueType> ReadShape(JsonDocument? metadataShape)
    {
        var allowed = new Dictionary<string, ProductAttributeValueType>(StringComparer.Ordinal);

        if (metadataShape is null)
        {
            return allowed;
        }

        if (!metadataShape.RootElement.TryGetProperty("fields", out var fields)
            || fields.ValueKind != JsonValueKind.Array)
        {
            return allowed;
        }

        foreach (var field in fields.EnumerateArray())
        {
            if (!field.TryGetProperty("key", out var keyElement)
                || !field.TryGetProperty("valueType", out var typeElement))
            {
                continue;
            }

            var key = keyElement.GetString();
            var rawType = typeElement.GetString();

            if (key is null || !Enum.TryParse<ProductAttributeValueType>(rawType, out var valueType))
            {
                continue;
            }

            allowed[key] = valueType;
        }

        return allowed;
    }

    private static object? Coerce(string key, ProductAttributeValueType type, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        switch (type)
        {
            case ProductAttributeValueType.Text:
                {
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        throw Mismatch(key, "text");
                    }

                    var text = value.GetString()?.Trim();
                    return string.IsNullOrEmpty(text) ? null : text;
                }

            case ProductAttributeValueType.Number:
                {
                    if (value.ValueKind != JsonValueKind.Number)
                    {
                        throw Mismatch(key, "a number");
                    }

                    return value.GetDecimal();
                }

            case ProductAttributeValueType.Boolean:
                {
                    if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    {
                        throw Mismatch(key, "true or false");
                    }

                    // Unchecked boxes are stored as false rather than dropped: for a
                    // yes/no field "false" is real information ("not spicy"), unlike
                    // an empty text box which just means unanswered.
                    return value.GetBoolean();
                }

            case ProductAttributeValueType.TextList:
                {
                    if (value.ValueKind != JsonValueKind.Array)
                    {
                        throw Mismatch(key, "a list of text values");
                    }

                    var items = new List<string>();

                    foreach (var item in value.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                        {
                            throw Mismatch(key, "a list of text values");
                        }

                        var text = item.GetString()?.Trim();

                        if (!string.IsNullOrEmpty(text))
                        {
                            items.Add(text);
                        }
                    }

                    return items.Count == 0 ? null : items;
                }

            default:
                return null;
        }
    }

    private static InvalidProductMetadataException Mismatch(string key, string expected) =>
        new($"'{key}' must be {expected}.");
}
