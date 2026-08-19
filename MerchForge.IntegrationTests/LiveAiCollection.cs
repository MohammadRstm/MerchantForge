namespace MerchForge.IntegrationTests;

/// <summary>
/// Runs every live-model class serially.
///
/// xunit parallelises across classes by default, which fired several requests at the
/// provider at once. Observed effect: a test that passes five times alone fails
/// intermittently in a full run - rate limiting and contention changing what comes
/// back, not the code under test. Serialising removes that noise, at the cost of a
/// slightly longer suite.
/// </summary>
[CollectionDefinition("Live AI", DisableParallelization = true)]
public class LiveAiCollection;
