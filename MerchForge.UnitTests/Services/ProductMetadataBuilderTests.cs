using System.Text.Json;
using FluentAssertions;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Services.BusinessDashboard;

namespace MerchForge.UnitTests.Services;

public class ProductMetadataBuilderTests
{
    private static JsonDocument Shape(string key, ProductAttributeValueType valueType, bool isRequired = false) =>
        JsonDocument.Parse($$"""
        { "fields": [ { "key": "{{key}}", "valueType": "{{valueType}}", "isRequired": {{(isRequired ? "true" : "false")}} } ] }
        """);

    private static Dictionary<string, JsonElement> Submitted(string key, object value)
    {
        var json = JsonSerializer.SerializeToElement(value);
        return new Dictionary<string, JsonElement> { [key] = json };
    }

    [Fact]
    public void ColorList_accepts_valid_hex_codes_and_normalizes_to_uppercase()
    {
        var shape = Shape("colors", ProductAttributeValueType.ColorList);
        var submitted = Submitted("colors", new[] { "#ff0000", "#00FF00" });

        var result = ProductMetadataBuilder.Build(shape, submitted);

        result.Should().NotBeNull();
        var colors = result!.RootElement.GetProperty("colors").EnumerateArray().Select(e => e.GetString()).ToArray();
        colors.Should().BeEquivalentTo(["#FF0000", "#00FF00"]);
    }

    [Fact]
    public void ColorList_rejects_a_value_that_is_not_a_hex_code()
    {
        var shape = Shape("colors", ProductAttributeValueType.ColorList);
        var submitted = Submitted("colors", new[] { "Black" });

        var act = () => ProductMetadataBuilder.Build(shape, submitted);

        act.Should().Throw<InvalidProductMetadataException>();
    }

    [Fact]
    public void ColorList_rejects_a_non_array_value()
    {
        var shape = Shape("colors", ProductAttributeValueType.ColorList);
        var submitted = Submitted("colors", "#FF0000");

        var act = () => ProductMetadataBuilder.Build(shape, submitted);

        act.Should().Throw<InvalidProductMetadataException>();
    }

    [Fact]
    public void ColorList_drops_blank_entries_and_returns_null_when_nothing_remains()
    {
        var shape = Shape("colors", ProductAttributeValueType.ColorList);
        var submitted = Submitted("colors", new[] { "  " });

        var result = ProductMetadataBuilder.Build(shape, submitted);

        result.Should().BeNull();
    }
}
