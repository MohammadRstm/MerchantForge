using MerchForge.api.Configurations;
using MerchForge.api.Services.AI.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace MerchForge.IntegrationTests;

/// <summary>
/// Builds a real conversation client from the API project's user secrets.
///
/// Reads the same secret store the application uses, so no key is ever committed,
/// duplicated into the test project, or passed on a command line. When no key is
/// configured the live tests skip rather than fail, so the suite still runs on a
/// machine without credentials.
/// </summary>
public class LiveAgentFixture
{
    public LiveAgentFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(OpenAiProductAiConversationClient).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        Options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
    }

    public AiOptions Options { get; }

    public bool IsConfigured => Options.IsConfigured;

    /// <summary>
    /// A fresh client per call. HttpClient is created directly rather than through a
    /// factory because these tests make a handful of requests in total; connection
    /// reuse is irrelevant at that volume.
    /// </summary>
    public OpenAiProductAiConversationClient CreateClient() =>
        new(new HttpClient { Timeout = TimeSpan.FromSeconds(60) }, Options.ToOptions());
}

internal static class AiOptionsExtensions
{
    public static IOptions<AiOptions> ToOptions(this AiOptions options) =>
        Microsoft.Extensions.Options.Options.Create(options);
}
