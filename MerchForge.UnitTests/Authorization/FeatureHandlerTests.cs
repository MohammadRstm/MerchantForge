using System.Security.Claims;
using FluentAssertions;
using MerchForge.api.Authorization.Handlers;
using MerchForge.api.Authorization.Requirements;
using MerchForge.api.Services.Subscription.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace MerchForge.UnitTests.Authorization;

/// <summary>
/// FeatureHandler is what actually gates every AI/website-customization
/// endpoint behind a business's plan or credit balance - these tests exist so a
/// bug here (e.g. reading the wrong claim, or defaulting to "succeed" instead of
/// "fail" on a malformed request) can't silently turn into every feature being
/// either wide open or permanently locked.
/// </summary>
public class FeatureHandlerTests
{
    private const string FeatureKey = "ai.image_editing";
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();

    private readonly Mock<ISubscriptionService> _subscriptionService = new();
    private readonly FeatureHandler _handler;

    public FeatureHandlerTests()
    {
        _handler = new FeatureHandler(_subscriptionService.Object);
    }

    private static ClaimsPrincipal AuthenticatedUser(Guid? userId) =>
        new(new ClaimsIdentity(
            userId is { } id ? [new Claim(ClaimTypes.NameIdentifier, id.ToString())] : [],
            "TestAuth"));

    private static HttpContext HttpContextWithBusinessId(object? businessIdRouteValue)
    {
        var httpContext = new DefaultHttpContext();
        if (businessIdRouteValue is not null)
        {
            httpContext.Request.RouteValues = new RouteValueDictionary { ["businessId"] = businessIdRouteValue };
        }
        return httpContext;
    }

    private async Task<AuthorizationHandlerContext> RunAsync(ClaimsPrincipal user, object? resource)
    {
        var requirement = new FeatureRequirement(FeatureKey);
        var context = new AuthorizationHandlerContext([requirement], user, resource);
        await _handler.HandleAsync(context);
        return context;
    }

    [Fact]
    public async Task Succeeds_when_the_business_has_the_feature()
    {
        _subscriptionService
            .Setup(s => s.HasFeatureAsync(BusinessId, FeatureKey))
            .ReturnsAsync(true);

        var context = await RunAsync(AuthenticatedUser(UserId), HttpContextWithBusinessId(BusinessId));

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_when_the_business_does_not_have_the_feature()
    {
        _subscriptionService
            .Setup(s => s.HasFeatureAsync(BusinessId, FeatureKey))
            .ReturnsAsync(false);

        var context = await RunAsync(AuthenticatedUser(UserId), HttpContextWithBusinessId(BusinessId));

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_without_a_parseable_user_id_claim()
    {
        var context = await RunAsync(AuthenticatedUser(null), HttpContextWithBusinessId(BusinessId));

        context.HasSucceeded.Should().BeFalse();
        _subscriptionService.Verify(
            s => s.HasFeatureAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Fails_when_the_resource_is_not_an_HttpContext()
    {
        var context = await RunAsync(AuthenticatedUser(UserId), resource: "not-an-http-context");

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_without_a_parseable_businessId_route_value()
    {
        var context = await RunAsync(AuthenticatedUser(UserId), HttpContextWithBusinessId(null));

        context.HasSucceeded.Should().BeFalse();
        _subscriptionService.Verify(
            s => s.HasFeatureAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Fails_when_the_businessId_route_value_is_not_a_guid()
    {
        var context = await RunAsync(AuthenticatedUser(UserId), HttpContextWithBusinessId("not-a-guid"));

        context.HasSucceeded.Should().BeFalse();
    }
}
