using MerchForge.api.Data;
using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace MerchForge.IntegrationTests;

/// <summary>
/// Creates a throwaway MariaDB database per test class run, applies the real
/// migrations to it, and drops it afterwards.
///
/// These are integration tests against the real provider on purpose. The things
/// worth protecting here — FK delete behaviour, the json column and whether metadata
/// survives a round trip with its value types intact, and business isolation in
/// actual SQL — are all provider behaviour. An in-memory or SQLite double would
/// happily pass while the real database did something else.
/// </summary>
public class CatalogDatabaseFixture : IAsyncLifetime
{
    private const string AdminConnectionString =
        "Server=localhost;Port=3306;Database=mysql;User=root;Password=;";

    private readonly string _databaseName =
        $"merchforge_test_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=localhost;Port=3306;Database={_databaseName};User=root;Password=;";

    // Seeded ids, from the migration's HasData.
    public static readonly Guid FashionDomainId = Guid.Parse("d1000000-0000-4000-8000-000000000001");
    public static readonly Guid RestaurantDomainId = Guid.Parse("d1000000-0000-4000-8000-000000000002");
    public static readonly Guid ShoesCategoryId = Guid.Parse("c1000000-0000-4000-8000-000000000001");
    public static readonly Guid ShirtsCategoryId = Guid.Parse("c1000000-0000-4000-8000-000000000002");
    public static readonly Guid PizzaCategoryId = Guid.Parse("c2000000-0000-4000-8000-000000000001");

    public async Task InitializeAsync()
    {
        await using (var admin = new MySqlConnection(AdminConnectionString))
        {
            await admin.OpenAsync();
            await new MySqlCommand($"CREATE DATABASE `{_databaseName}`;", admin)
                .ExecuteNonQueryAsync();
        }

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var admin = new MySqlConnection(AdminConnectionString);
        await admin.OpenAsync();
        await new MySqlCommand($"DROP DATABASE IF EXISTS `{_databaseName}`;", admin)
            .ExecuteNonQueryAsync();
    }

    public MerchForgeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MerchForgeDbContext>()
            .UseMySql(
                ConnectionString,
                new MariaDbServerVersion(new Version(10, 4, 32)),
                mySql => mySql.UseMicrosoftJson())
            .Options;

        return new MerchForgeDbContext(options);
    }

    /// <summary>
    /// Creates a user + business pair. Businesses need an owner, so tests that only
    /// care about the catalog still need one.
    /// </summary>
    public async Task<Business> CreateBusinessAsync(
        string name,
        Guid? domainId,
        string currency = "USD")
    {
        await using var db = CreateContext();

        var systemRoleId = await db.SystemRoles
            .Where(r => r.Role == api.Enums.SystemRole.User)
            .Select(r => r.Id)
            .FirstAsync();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Owner",
            LastName = name,
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = "not-a-real-hash",
            SystemRoleId = systemRoleId,
        };

        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerUserId = owner.Id,
            BusinessDomainId = domainId,
            Currency = currency,
            Locale = "en-US",
        };

        db.Users.Add(owner);
        db.Businesses.Add(business);
        await db.SaveChangesAsync();

        return business;
    }

    public async Task<Product> CreateProductAsync(
        Guid businessId,
        Guid categoryId,
        string title,
        decimal price,
        string? metadataJson = null,
        DateTime? createdAt = null)
    {
        await using var db = CreateContext();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = categoryId,
            Title = title,
            Description = $"{title} description.",
            Price = price,
            Metadata = metadataJson is null
                ? null
                : System.Text.Json.JsonDocument.Parse(metadataJson),
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = createdAt ?? DateTime.UtcNow,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return product;
    }
}
