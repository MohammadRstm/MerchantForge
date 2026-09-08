using System.Security.Claims;
using System.Net;
using FluentAssertions;
using MerchForge.api.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MerchForge.UnitTests.RateLimiting;

/// <summary>
/// The partition-key logic Program.cs's rate-limit policies key on. Covered
/// directly against a constructed HttpContext rather than through the full HTTP
/// pipeline (this repo has no WebApplicationFactory-based tests yet) - what
/// matters here is that each boundary reads the right value and degrades
/// predictably when it's missing, which is exactly where a partitioning bug
/// would hide.
/// </summary>
public class RateLimitPartitionsTests
{
    [Fact]
    public void GetClientIpPartitionKey_returns_the_remote_ip()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.5");

        RateLimitPartitions.GetClientIpPartitionKey(context).Should().Be("203.0.113.5");
    }

    [Fact]
    public void GetClientIpPartitionKey_distinguishes_different_ips()
    {
        var contextA = new DefaultHttpContext();
        contextA.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.5");

        var contextB = new DefaultHttpContext();
        contextB.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.9");

        RateLimitPartitions.GetClientIpPartitionKey(contextA)
            .Should().NotBe(RateLimitPartitions.GetClientIpPartitionKey(contextB));
    }

    [Fact]
    public void GetClientIpPartitionKey_falls_back_to_unknown_when_no_remote_address_is_available()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;

        RateLimitPartitions.GetClientIpPartitionKey(context).Should().Be("unknown");
    }

    [Fact]
    public void GetBusinessPartitionKey_reads_the_businessId_route_value()
    {
        var businessId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            Request = { RouteValues = new RouteValueDictionary { ["businessId"] = businessId } },
        };

        RateLimitPartitions.GetBusinessPartitionKey(context).Should().Be(businessId.ToString());
    }

    [Fact]
    public void GetBusinessPartitionKey_distinguishes_different_businesses()
    {
        var contextA = new DefaultHttpContext
        {
            Request = { RouteValues = new RouteValueDictionary { ["businessId"] = Guid.NewGuid() } },
        };
        var contextB = new DefaultHttpContext
        {
            Request = { RouteValues = new RouteValueDictionary { ["businessId"] = Guid.NewGuid() } },
        };

        RateLimitPartitions.GetBusinessPartitionKey(contextA)
            .Should().NotBe(RateLimitPartitions.GetBusinessPartitionKey(contextB));
    }

    [Fact]
    public void GetBusinessPartitionKey_falls_back_to_unknown_when_the_route_has_no_businessId()
    {
        var context = new DefaultHttpContext();

        RateLimitPartitions.GetBusinessPartitionKey(context).Should().Be("unknown");
    }

    [Fact]
    public void GetStorefrontBusinessPartitionKey_reads_the_businessId_query_parameter()
    {
        var businessId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString($"?businessId={businessId}");

        RateLimitPartitions.GetStorefrontBusinessPartitionKey(context).Should().Be(businessId.ToString());
    }

    [Fact]
    public void GetStorefrontBusinessPartitionKey_distinguishes_different_storefronts()
    {
        var contextA = new DefaultHttpContext();
        contextA.Request.QueryString = new QueryString($"?businessId={Guid.NewGuid()}");

        var contextB = new DefaultHttpContext();
        contextB.Request.QueryString = new QueryString($"?businessId={Guid.NewGuid()}");

        RateLimitPartitions.GetStorefrontBusinessPartitionKey(contextA)
            .Should().NotBe(RateLimitPartitions.GetStorefrontBusinessPartitionKey(contextB));
    }

    [Fact]
    public void GetStorefrontBusinessPartitionKey_falls_back_to_unknown_when_the_query_has_no_businessId()
    {
        var context = new DefaultHttpContext();

        RateLimitPartitions.GetStorefrontBusinessPartitionKey(context).Should().Be("unknown");
    }

    [Fact]
    public void GetUserPartitionKey_reads_the_authenticated_user_id_claim()
    {
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test")),
        };

        RateLimitPartitions.GetUserPartitionKey(context).Should().Be(userId.ToString());
    }

    /// <summary>
    /// The SuperAdmin dashboard reads across every business, so there is no tenant
    /// boundary available - two admins must still land in separate buckets, or one
    /// of them can throttle the other.
    /// </summary>
    [Fact]
    public void GetUserPartitionKey_distinguishes_different_admins()
    {
        static DefaultHttpContext ContextFor(Guid id) => new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "test")),
        };

        RateLimitPartitions.GetUserPartitionKey(ContextFor(Guid.NewGuid()))
            .Should().NotBe(RateLimitPartitions.GetUserPartitionKey(ContextFor(Guid.NewGuid())));
    }

    /// <summary>
    /// Every endpoint using this policy sits behind an authorization policy, so an
    /// anonymous context should not arise - but the fallback must still be a real
    /// key rather than null, which would throw inside the limiter. Lumping unknown
    /// callers together is the safe direction: shared bucket, not no bucket.
    /// </summary>
    [Fact]
    public void GetUserPartitionKey_falls_back_to_unknown_for_an_unauthenticated_context()
    {
        RateLimitPartitions.GetUserPartitionKey(new DefaultHttpContext()).Should().Be("unknown");
    }
}
