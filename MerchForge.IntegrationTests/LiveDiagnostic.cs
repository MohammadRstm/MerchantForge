using System.Text.Json;
using MerchForge.api.Services.AI.Contracts;
using Xunit.Abstractions;

namespace MerchForge.IntegrationTests;

/// <summary>
/// Prints a raw decision for one prompt. Not an assertion - a way to see what the
/// model actually returned when a behaviour test disagrees with expectations.
/// Skipped unless explicitly filtered for, so it never adds cost to a normal run.
/// </summary>
[Collection("Live AI")]
public class LiveDiagnostic : IClassFixture<LiveAgentFixture>
{
    private readonly LiveAgentFixture _fixture;
    private readonly ITestOutputHelper _output;

    public LiveDiagnostic(LiveAgentFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [SkippableFact(Skip = "Diagnostic only; run explicitly when investigating a behaviour.")]
    public async Task Dump()
    {
        Skip.IfNot(_fixture.IsConfigured, "No AI provider configured.");

        var context = new ProductAiContext
        {
            BusinessName = "Test Apparel",
            Currency = "USD",
            Categories = [new ProductAiCategory { Id = Guid.NewGuid(), Name = "Shirts" }],
            MetadataFields =
            [
                new ProductAiField { Key = "colors", Label = "Colors", ValueType = "TextList", IsRequired = true,
                    AllowedValues = ["Black", "White", "Red", "Blue", "Green"] },
                new ProductAiField { Key = "sizes", Label = "Sizes", ValueType = "TextList", IsRequired = true,
                    AllowedValues = ["XS", "S", "M", "L", "XL", "XXL"] },
                new ProductAiField { Key = "material", Label = "Material", ValueType = "Text" },
                new ProductAiField { Key = "brand", Label = "Brand", ValueType = "Text" },
            ],
            LatestUserMessage =
                "Add this black cotton hoodie. It's from our winter collection, sizes S M L XL, $59.99, "
                + "made from 80% cotton and 20% polyester. The brand is ABC and I want the background "
                + "removed and replaced with a white studio background.",
            HasImage = true,
        };

        var result = await _fixture.CreateClient().ContinueConversationAsync(context);
        var d = result.Decision;

        _output.WriteLine($"action={d.Action}");
        _output.WriteLine($"imagePrompt={d.ImageModificationPrompt}");
        _output.WriteLine($"title={d.Draft?.Title}");
        _output.WriteLine($"price={d.Draft?.Price}");
        _output.WriteLine("metadata=" + JsonSerializer.Serialize(d.Draft?.Metadata));
    }
}
