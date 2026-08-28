using System.Text.Json;

namespace MerchForge.api.Services.Common;

/// <summary>
/// Reads/writes the per-template sub-object inside Business.WebsiteCustomizationValues
/// (and the structurally identical BusinessWebsiteDraft.TemplateFieldsDraft), namespaced
/// by WebsiteTemplateId — see Business.WebsiteCustomizationValues's own doc comment for
/// why the namespacing exists. Mirrors MetadataShapeReader's role as the one shared
/// place this shape is parsed.
/// </summary>
public static class WebsiteCustomizationValuesReader
{
    /// <summary>The sub-object for one template, or null if that template has no saved values yet.</summary>
    public static JsonDocument? ReadForTemplate(JsonDocument? values, Guid? websiteTemplateId)
    {
        if (values is null
            || websiteTemplateId is not Guid templateId
            || values.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!values.RootElement.TryGetProperty(templateId.ToString(), out var sub)
            || sub.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return JsonDocument.Parse(sub.GetRawText());
    }

    /// <summary>
    /// Replaces only the given template's sub-object, preserving every other
    /// template's leftover values untouched — switching back to a previously-used
    /// template recovers its old values for free, by design.
    /// </summary>
    public static JsonDocument? WriteForTemplate(
        JsonDocument? existingValues,
        Guid websiteTemplateId,
        JsonDocument? newTemplateValues)
    {
        var root = new Dictionary<string, JsonElement>();

        if (existingValues is not null && existingValues.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in existingValues.RootElement.EnumerateObject())
            {
                root[property.Name] = property.Value.Clone();
            }
        }

        var key = websiteTemplateId.ToString();

        if (newTemplateValues is null)
        {
            root.Remove(key);
        }
        else
        {
            root[key] = newTemplateValues.RootElement.Clone();
        }

        return root.Count == 0 ? null : JsonSerializer.SerializeToDocument(root);
    }
}
