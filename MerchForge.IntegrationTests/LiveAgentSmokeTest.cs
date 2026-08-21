using FluentAssertions;
using MerchForge.api.Services.AI.Contracts;

namespace MerchForge.IntegrationTests;

/// <summary>
/// One request against the real provider, to prove the request shape, the strict
/// json_schema and the response parsing all line up before any further calls are
/// made. Kept separate and minimal so a contract mistake costs one call, not thirty.
/// </summary>
[Collection("Live AI")]
public class LiveAgentSmokeTest : IClassFixture<LiveAgentFixture>
{
    private readonly LiveAgentFixture _fixture;

    public LiveAgentSmokeTest(LiveAgentFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task The_provider_returns_a_decision_matching_our_schema()
    {
        Skip.IfNot(_fixture.IsConfigured, "No AI provider configured.");

        var context = new ProductAiContext
        {
            BusinessName = "Smoke Test Store",
            Currency = "USD",
            Categories = [new ProductAiCategory { Id = Guid.NewGuid(), Name = "Shirts" }],
            MetadataFields =
            [
                new ProductAiField
                {
                    Key = "colors", Label = "Colors", ValueType = "TextList",
                    IsRequired = true, AllowedValues = ["Black", "White"],
                },
            ],
            CurrentDraft = null,
            History = [],
            LatestUserMessage = "A black shirt for $20.",
        };

        var result = await _fixture.CreateClient().ContinueConversationAsync(context);

        // Only that the contract holds - not what the model decided, which later tests
        // examine in detail.
        result.Should().NotBeNull();

        // Message is deliberately not asserted non-empty: the model sometimes returns
        // an empty one, and the service supplies a fallback reply rather than relying
        // on it. What matters here is that the decision parses at all.
        result.Decision.Message.Should().NotBeNull();
        result.Decision.Action.Should().BeOneOf(
            ProductAiAction.RequestInformation,
            ProductAiAction.UpdateDraft,
            ProductAiAction.ReadyForReview,
            ProductAiAction.Cancel);

        // Token usage is read back, which the interaction logger depends on.
        result.PromptTokens.Should().BeGreaterThan(0);
    }
}
