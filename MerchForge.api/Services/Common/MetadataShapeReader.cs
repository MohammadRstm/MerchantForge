using System.Text.Json;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Services.Common;

/// <summary>
/// Parses a Business.MetadataShape JSON snapshot into the form the product form (and
/// the SuperAdmin metadata-shape editor) render fields from. Shared by
/// BusinessDashboardService and DashboardService so there is exactly one reading of
/// this format.
/// </summary>
public static class MetadataShapeReader
{
    public static List<ProductFormFieldResponse> Read(JsonDocument? metadataShape)
    {
        var fields = new List<ProductFormFieldResponse>();

        if (metadataShape is null
            || !metadataShape.RootElement.TryGetProperty("fields", out var fieldsElement)
            || fieldsElement.ValueKind != JsonValueKind.Array)
        {
            return fields;
        }

        foreach (var field in fieldsElement.EnumerateArray())
        {
            var key = field.TryGetProperty("key", out var k) ? k.GetString() : null;
            var label = field.TryGetProperty("label", out var l) ? l.GetString() : null;
            var valueType = field.TryGetProperty("valueType", out var v) ? v.GetString() : null;

            if (key is null || label is null || valueType is null)
            {
                continue;
            }

            fields.Add(new ProductFormFieldResponse
            {
                Key = key,
                Label = label,
                ValueType = valueType,
                IsRequired = field.TryGetProperty("isRequired", out var req)
                    && req.ValueKind == JsonValueKind.True,
                AllowedValues = field.TryGetProperty("allowedValues", out var allowed)
                    && allowed.ValueKind == JsonValueKind.Array
                        ? allowed.EnumerateArray()
                            .Where(v => v.ValueKind == JsonValueKind.String)
                            .Select(v => v.GetString()!)
                            .ToList()
                        : [],
            });
        }

        return fields;
    }
}
