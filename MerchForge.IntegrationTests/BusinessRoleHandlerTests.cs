using System.Security.Claims;
using FluentAssertions;
using MerchForge.api.Authorization.Handlers;
using MerchForge.api.Authorization.Requirements;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// BusinessRoleHandler is the one thing standing between a business-scoped route
/// (products, orders, inventory, everything under
/// api/businesses/{businessId}/dashboard/...) and a caller from a different
/// business - it re-derives membership from the database on every request rather
/// than trusting the token, and these tests exist to lock that property in place.
/// Against the real database on purpose, same reasoning as the rest of this
/// project's integration tests: role lookups and membership joins are exactly the
/// kind of thing a mock would happily fake correctly while a real query does
/// something subtly different.
/// </summary>
public class BusinessRoleHandlerTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;
    private Business _businessA = null!;
    private Business _businessB = null!;
    private User _ownerOfA = null!;
    private User _memberOfA = null!;

    public BusinessRoleHandlerTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _businessA = await _fixture.CreateBusinessAsync("Business A", CatalogDatabaseFixture.FashionDomainId);
        _businessB = await _fixture.CreateBusinessAsync("Business B", CatalogDatabaseFixture.FashionDomainId);

        await using var db = _fixture.CreateContext();

        var ownerRoleId = await db.BusinessUserRoles
            .Where(r => r.Role == BusinessRole.Owner)
            .Select(r => r.Id)
            .FirstAsync();

        var memberRoleId = await db.BusinessUserRoles
            .Where(r => r.Role == BusinessRole.Member)
            .Select(r => r.Id)
            .FirstAsync();

        var systemRoleId = await db.SystemRoles
            .Where(r => r.Role == SystemRole.User)
            .Select(r => r.Id)
            .FirstAsync();

        _ownerOfA = await db.Users.FirstAsync(u => u.Id == _businessA.OwnerUserId);

        _memberOfA = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Team",
            LastName = "Member",
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = "not-a-real-hash",
            SystemRoleId = systemRoleId,
        };
        db.Users.Add(_memberOfA);

        db.BusinessUsers.Add(new BusinessUser
        {
            UserId = _ownerOfA.Id,
            BusinessId = _businessA.Id,
            RoleId = ownerRoleId,
        });
        db.BusinessUsers.Add(new BusinessUser
        {
            UserId = _memberOfA.Id,
            BusinessId = _businessA.Id,
            RoleId = memberRoleId,
        });

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

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

    private async Task<AuthorizationHandlerContext> RunAsync(
        ClaimsPrincipal user, object? resource, params BusinessRole[] allowedRoles)
    {
        await using var db = _fixture.CreateContext();
        var handler = new BusinessRoleHandler(db, new UserRepository(db));

        var requirement = new BusinessRoleRequirements(allowedRoles);
        var context = new AuthorizationHandlerContext([requirement], user, resource);
        await handler.HandleAsync(context);
        return context;
    }

    [Fact]
    public async Task Owner_passes_an_owner_only_requirement_for_their_own_business()
    {
        var context = await RunAsync(
            AuthenticatedUser(_ownerOfA.Id), HttpContextWithBusinessId(_businessA.Id), BusinessRole.Owner);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Member_fails_an_owner_only_requirement()
    {
        var context = await RunAsync(
            AuthenticatedUser(_memberOfA.Id), HttpContextWithBusinessId(_businessA.Id), BusinessRole.Owner);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Member_passes_a_member_or_admin_or_owner_requirement()
    {
        var context = await RunAsync(
            AuthenticatedUser(_memberOfA.Id),
            HttpContextWithBusinessId(_businessA.Id),
            BusinessRole.Member, BusinessRole.Admin, BusinessRole.Owner);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Owner_of_one_business_cannot_pass_for_a_route_naming_a_different_business()
    {
        // The exact property this handler exists to guarantee: a route's businessId
        // is checked against a fresh membership lookup, never trusted from the
        // caller. _ownerOfA has no BusinessUser row at all for _businessB.
        var context = await RunAsync(
            AuthenticatedUser(_ownerOfA.Id), HttpContextWithBusinessId(_businessB.Id), BusinessRole.Owner);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_for_a_user_with_no_membership_in_the_named_business()
    {
        var stranger = Guid.NewGuid();

        var context = await RunAsync(
            AuthenticatedUser(stranger), HttpContextWithBusinessId(_businessA.Id), BusinessRole.Owner);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_without_a_parseable_businessId_route_value()
    {
        var context = await RunAsync(
            AuthenticatedUser(_ownerOfA.Id), HttpContextWithBusinessId(null), BusinessRole.Owner);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_when_the_resource_is_not_an_HttpContext()
    {
        var context = await RunAsync(
            AuthenticatedUser(_ownerOfA.Id), resource: "not-an-http-context", BusinessRole.Owner);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_without_a_parseable_user_id_claim()
    {
        var context = await RunAsync(
            AuthenticatedUser(null), HttpContextWithBusinessId(_businessA.Id), BusinessRole.Owner);

        context.HasSucceeded.Should().BeFalse();
    }
}
