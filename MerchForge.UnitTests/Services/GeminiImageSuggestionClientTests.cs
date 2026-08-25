using FluentAssertions;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Services.AI.Providers;

namespace MerchForge.UnitTests.Services;

public class GeminiImageSuggestionClientTests
{
    private static string Response(string outputText) => $$"""
        { "output_text": { "type": "text", "text": {{System.Text.Json.JsonSerializer.Serialize(outputText)}} } }
        """;

    [Fact]
    public void Parses_a_plain_json_object_from_output_text()
    {
        var body = Response("""{"title": "Blue mug", "description": null, "price": null, "compareAtPrice": null, "categoryId": null, "sku": null, "stockQuantity": null, "tags": ["ceramic"], "saleEndsAt": null, "metadata": null}""");

        var draft = GeminiImageSuggestionClient.ParseSuggestion(body);

        draft.Title.Should().Be("Blue mug");
        draft.Description.Should().BeNull();
        draft.Tags.Should().BeEquivalentTo(["ceramic"]);
    }

    [Fact]
    public void Strips_a_markdown_code_fence_around_the_json()
    {
        var fenced = "```json\n{\"title\": \"Red scarf\", \"tags\": []}\n```";
        var body = Response(fenced);

        var draft = GeminiImageSuggestionClient.ParseSuggestion(body);

        draft.Title.Should().Be("Red scarf");
        draft.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Falls_back_to_scanning_step_content_for_a_text_part_when_output_text_is_absent()
    {
        var body = """
            {
              "steps": [
                { "type": "reasoning", "content": [ { "type": "text", "text": "thinking..." } ] },
                { "type": "final", "content": [ { "type": "text", "text": "{\"title\": \"Green vase\", \"tags\": []}" } ] }
              ]
            }
            """;

        var draft = GeminiImageSuggestionClient.ParseSuggestion(body);

        draft.Title.Should().Be("Green vase");
    }

    [Fact]
    public void Defaults_tags_to_an_empty_list_when_the_model_omits_it()
    {
        var body = Response("""{"title": "Yellow hat"}""");

        var draft = GeminiImageSuggestionClient.ParseSuggestion(body);

        draft.Tags.Should().NotBeNull();
        draft.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Throws_a_clear_error_when_no_text_content_is_present_anywhere()
    {
        var body = """{ "steps": [] }""";

        var act = () => GeminiImageSuggestionClient.ParseSuggestion(body);

        act.Should().Throw<ImageEditingException>();
    }

    [Fact]
    public void Throws_a_clear_error_when_the_text_is_not_valid_json()
    {
        var body = Response("Sorry, I can't help with that.");

        var act = () => GeminiImageSuggestionClient.ParseSuggestion(body);

        act.Should().Throw<ImageEditingException>();
    }
}
