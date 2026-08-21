using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace MerchForge.IntegrationTests.Fakes;

/// <summary>
/// No-op stand-in for Hangfire's job client. Nothing under test here enqueues a real
/// background job (the admin website-template-choice notification lives on a
/// different service path), so this only needs to satisfy the constructor — there is
/// nothing to record or assert against.
/// </summary>
public class FakeBackgroundJobClient : IBackgroundJobClient
{
    public string Create(Job job, IState state) => Guid.NewGuid().ToString();

    public bool ChangeState(string jobId, IState state, string? fromState) => true;
}
