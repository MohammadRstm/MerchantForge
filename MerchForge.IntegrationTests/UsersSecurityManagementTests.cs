using FluentAssertions;
using MerchForge.api.Data;
using MerchForge.api.DTOs.Audit;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.IntegrationTests;

/// <summary>
/// The Super Admin Users &amp; Security enhancement's new repository logic: the
/// multi-business-membership fix (the old code threw on a second membership row
/// per user), the new Users filters, the account-disable data, and the new
/// AuditLog table's write/query path. Real MariaDB, matching this suite's
/// established convention.
/// </summary>
public class UsersSecurityManagementTests : IClassFixture<CatalogDatabaseFixture>, IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _fixture;

    private Business _businessA = null!;
    private Business _businessB = null!;

    public UsersSecurityManagementTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _businessA = await _fixture.CreateBusinessAsync("Users Test Co A", CatalogDatabaseFixture.FashionDomainId);
        _businessB = await _fixture.CreateBusinessAsync("Users Test Co B", CatalogDatabaseFixture.FashionDomainId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<User> CreatePlatformUserAsync(MerchForgeDbContext db, SystemRole role = SystemRole.User)
    {
        var systemRoleId = await db.SystemRoles.Where(r => r.Role == role).Select(r => r.Id).FirstAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = $"User-{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = "not-a-real-hash",
            SystemRoleId = systemRoleId,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private async Task AddMembershipAsync(MerchForgeDbContext db, Guid userId, Guid businessId, BusinessRole role, DateTime? createdAt = null)
    {
        var roleId = await db.BusinessUserRoles.Where(r => r.Role == role).Select(r => r.Id).FirstAsync();

        db.BusinessUsers.Add(new BusinessUser
        {
            UserId = userId,
            BusinessId = businessId,
            RoleId = roleId,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = createdAt ?? DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUsersAsync_does_not_throw_for_a_user_belonging_to_more_than_one_business()
    {
        await using var db = _fixture.CreateContext();
        var user = await CreatePlatformUserAsync(db);

        await AddMembershipAsync(db, user.Id, _businessA.Id, BusinessRole.Owner, DateTime.UtcNow.AddDays(-2));
        await AddMembershipAsync(db, user.Id, _businessB.Id, BusinessRole.Member, DateTime.UtcNow.AddDays(-1));

        var repository = new DashboardRepository(db);

        var act = () => repository.GetUsersAsync(new UsersQueryRequest { Search = user.Email, PageSize = 50 });

        (await act.Should().NotThrowAsync()).Which.Items
            .Should().ContainSingle(u => u.Id == user.Id)
            .Which.AdditionalMembershipCount.Should().Be(1, "one membership beyond the primary (earliest-joined) one");
    }

    [Fact]
    public async Task GetUsersAsync_reports_the_earliest_membership_as_the_primary_business()
    {
        await using var db = _fixture.CreateContext();
        var user = await CreatePlatformUserAsync(db);

        await AddMembershipAsync(db, user.Id, _businessB.Id, BusinessRole.Member, DateTime.UtcNow.AddDays(-1));
        await AddMembershipAsync(db, user.Id, _businessA.Id, BusinessRole.Owner, DateTime.UtcNow.AddDays(-5));

        var repository = new DashboardRepository(db);
        var (items, _) = await repository.GetUsersAsync(new UsersQueryRequest { Search = user.Email, PageSize = 50 });

        items.Should().ContainSingle().Which.BusinessName.Should().Be(_businessA.Name);
    }

    [Fact]
    public async Task GetUsersAsync_filters_by_business_role_and_account_status()
    {
        await using var db = _fixture.CreateContext();
        var owner = await CreatePlatformUserAsync(db);
        var member = await CreatePlatformUserAsync(db);
        var disabled = await CreatePlatformUserAsync(db);
        disabled.DisabledAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await AddMembershipAsync(db, owner.Id, _businessA.Id, BusinessRole.Owner);
        await AddMembershipAsync(db, member.Id, _businessA.Id, BusinessRole.Member);

        var repository = new DashboardRepository(db);

        var (owners, _) = await repository.GetUsersAsync(new UsersQueryRequest { BusinessRole = BusinessRole.Owner, PageSize = 200 });
        owners.Should().Contain(u => u.Id == owner.Id);
        owners.Should().NotContain(u => u.Id == member.Id);

        var (disabledUsers, _) = await repository.GetUsersAsync(new UsersQueryRequest { IsDisabled = true, PageSize = 200 });
        disabledUsers.Should().Contain(u => u.Id == disabled.Id && u.IsDisabled);
        disabledUsers.Should().NotContain(u => u.Id == owner.Id);
    }

    [Fact]
    public async Task GetUserDetailAsync_returns_every_membership_not_just_one()
    {
        await using var db = _fixture.CreateContext();
        var user = await CreatePlatformUserAsync(db);

        await AddMembershipAsync(db, user.Id, _businessA.Id, BusinessRole.Owner);
        await AddMembershipAsync(db, user.Id, _businessB.Id, BusinessRole.Member);

        var repository = new DashboardRepository(db);
        var detail = await repository.GetUserDetailAsync(user.Id);

        detail.Should().NotBeNull();
        detail!.Memberships.Should().HaveCount(2);
        detail.Memberships.Should().Contain(m => m.BusinessId == _businessA.Id && m.BusinessRole == "Owner");
        detail.Memberships.Should().Contain(m => m.BusinessId == _businessB.Id && m.BusinessRole == "Member");
    }

    [Fact]
    public async Task RevokeAllAsync_excludes_the_acting_users_own_session()
    {
        await using var db = _fixture.CreateContext();
        var actingAdmin = await CreatePlatformUserAsync(db, SystemRole.SuperAdmin);
        var otherUser = await CreatePlatformUserAsync(db);

        var now = DateTime.UtcNow;
        db.RefreshTokens.AddRange(
            new RefreshToken { Id = Guid.NewGuid(), UserId = actingAdmin.Id, TokenHash = $"h-{Guid.NewGuid():N}", ExpiresAt = now.AddDays(1) },
            new RefreshToken { Id = Guid.NewGuid(), UserId = otherUser.Id, TokenHash = $"h-{Guid.NewGuid():N}", ExpiresAt = now.AddDays(1) });
        await db.SaveChangesAsync();

        var repository = new RefreshTokenRepository(db);
        await repository.RevokeAllAsync(actingAdmin.Id);

        var adminToken = await db.RefreshTokens.AsNoTracking().FirstAsync(t => t.UserId == actingAdmin.Id);
        var otherToken = await db.RefreshTokens.AsNoTracking().FirstAsync(t => t.UserId == otherUser.Id);

        adminToken.RevokedAt.Should().BeNull("the acting Super Admin's own session must survive a platform-wide revoke");
        otherToken.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AuditLogRepository_writes_and_filters_by_event_type_and_business()
    {
        await using var db = _fixture.CreateContext();
        var repository = new AuditLogRepository(db);

        var businessScoped = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorDisplayName = "Test Actor",
            EventType = AuditEventType.Subscription,
            Action = "BusinessSubscriptionChanged",
            Description = "Changed a business's subscription to Pro.",
            Success = true,
            BusinessId = _businessA.Id,
            CreatedAt = DateTime.UtcNow,
        };
        var authEvent = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorDisplayName = "nobody@example.com",
            EventType = AuditEventType.Authentication,
            Action = "LoginFailed",
            Description = "Failed login attempt.",
            Success = false,
            CreatedAt = DateTime.UtcNow,
        };

        await repository.AddAsync(businessScoped);
        await repository.AddAsync(authEvent);

        var (byBusiness, _) = await repository.GetLogsAsync(new AuditLogQueryRequest { BusinessId = _businessA.Id, PageSize = 50 });
        byBusiness.Should().Contain(l => l.Id == businessScoped.Id);
        byBusiness.Should().NotContain(l => l.Id == authEvent.Id);
        byBusiness.Single(l => l.Id == businessScoped.Id).BusinessName.Should().Be(_businessA.Name);

        var (byEventType, _) = await repository.GetLogsAsync(new AuditLogQueryRequest { EventType = AuditEventType.Authentication, PageSize = 50 });
        byEventType.Should().Contain(l => l.Id == authEvent.Id);
        byEventType.Should().NotContain(l => l.Id == businessScoped.Id);

        var (bySuccess, _) = await repository.GetLogsAsync(new AuditLogQueryRequest { Success = false, PageSize = 50 });
        bySuccess.Should().Contain(l => l.Id == authEvent.Id);
    }
}
