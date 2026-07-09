using System;
using System.IO;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using BaGetter.Database.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Tests;

/// <summary>
/// Verifies that the AddMultiFeedSupport migration correctly upgrades a pre-existing database:
///   • All existing packages get FeedId = the default feed Guid (000...0001)
///   • A Feeds row is seeded for the default feed (Id = 000...0001, Slug = "default")
///   • FeedPermissions rows whose FeedId was the string "default" are rewritten to 000...0001
///   • FeedPermissions rows with unknown FeedId values are deleted
/// </summary>
public class MultiFeedUpgradeTests : IDisposable
{
    private const string DefaultFeedIdString = "00000000-0000-0000-0000-000000000001";

    private readonly string _dbPath;
    private readonly string _connectionString;

    public MultiFeedUpgradeTests()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "BaGetterMigrationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        _dbPath = Path.Combine(tempDir, "upgrade-test.db");
        _connectionString = $"Data Source={_dbPath}";
    }

    [Fact]
    public async Task ExistingPackages_GetDefaultFeedId_AfterMigration()
    {
        await ApplyMigrationsUpto("20260408095906_AddAuthEntities");
        SeedPreUpgradeData();

        await ApplyAllMigrations();

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT FeedId FROM Packages";
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read(), "Expected at least one package row");
        Assert.Equal(DefaultFeedIdString, reader.GetString(0), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DefaultFeedRow_IsSeeded_AfterMigration()
    {
        await ApplyMigrationsUpto("20260408095906_AddAuthEntities");
        SeedPreUpgradeData();

        await ApplyAllMigrations();

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Slug FROM Feeds WHERE Slug = 'default'";
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read(), "Expected a default feed row in Feeds table");
        Assert.Equal(DefaultFeedIdString, reader.GetString(0), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("default", reader.GetString(1));
    }

    [Fact]
    public async Task FeedPermissions_WithStringDefaultFeedId_AreRemapped_AfterMigration()
    {
        await ApplyMigrationsUpto("20260408095906_AddAuthEntities");
        SeedPreUpgradeData();

        await ApplyAllMigrations();

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT FeedId FROM FeedPermissions";
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read(), "Expected at least one FeedPermission row");
        Assert.Equal(DefaultFeedIdString, reader.GetString(0), StringComparer.OrdinalIgnoreCase);
        // There should be exactly one row (the unknown-feed row was deleted)
        Assert.False(reader.Read(), "Expected exactly one FeedPermission row after migration");
    }

    [Fact]
    public async Task FeedPermissions_WithUnknownFeedId_AreDeleted_AfterMigration()
    {
        await ApplyMigrationsUpto("20260408095906_AddAuthEntities");
        SeedPreUpgradeData();

        await ApplyAllMigrations();

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM FeedPermissions WHERE FeedId NOT IN (SELECT Id FROM Feeds)";
        var count = (long)cmd.ExecuteScalar();

        Assert.Equal(0, count);
    }

    /// <summary>
    /// Seeds the pre-upgrade state:
    ///   • One Package (no FeedId column yet)
    ///   • One FeedPermission with FeedId = 'default' (the old string slug)
    ///   • One FeedPermission with FeedId = 'unknown-feed' (should be deleted by migration)
    /// </summary>
    private void SeedPreUpgradeData()
    {
        using var conn = OpenConnection();

        // Minimal Package row. Note: the EF property NormalizedVersionString maps to column "Version";
        // there is no "NormalizedVersionString" column in the physical schema at this migration point.
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO Packages (Id, Downloads, HasReadme, HasEmbeddedIcon,
                    IsPrerelease, Listed, Published, RequireLicenseAcceptance,
                    SemVerLevel, Version)
                VALUES ('TestPackage', 0, 0, 0, 0, 1, '2020-01-01', 0, 0, '1.0.0')";
            cmd.ExecuteNonQuery();
        }

        // FeedPermission with string slug 'default' (expected to be remapped)
        var principalId = Guid.NewGuid().ToString();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO FeedPermissions (Id, FeedId, PrincipalType, PrincipalId, CanPush, CanPull)
                VALUES ($id, 'default', 0, $principalId, 1, 1)";
            cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("$principalId", principalId);
            cmd.ExecuteNonQuery();
        }

        // FeedPermission with unknown FeedId (expected to be deleted)
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO FeedPermissions (Id, FeedId, PrincipalType, PrincipalId, CanPush, CanPull)
                VALUES ($id, 'unknown-feed', 0, $principalId, 0, 1)";
            cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("$principalId", Guid.NewGuid().ToString());
            cmd.ExecuteNonQuery();
        }
    }

    private async Task ApplyMigrationsUpto(string targetMigration)
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SqliteContext>();
        await ctx.Database.GetService<IMigrator>().MigrateAsync(targetMigration);
    }

    private async Task ApplyAllMigrations()
    {
        using var sp = BuildServiceProvider();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SqliteContext>();
        await ctx.Database.MigrateAsync();
    }

    private ServiceProvider BuildServiceProvider()
    {
        // Provide a stub IOptionsSnapshot<BaGetterOptions> so SqliteContext can be resolved.
        // OnConfiguring checks !optionsBuilder.IsConfigured, which is false when we supply
        // options via AddDbContext — so the connection string from AddDbContext wins.
        var bagetterOptions = new BaGetterOptions
        {
            Database = new DatabaseOptions { ConnectionString = _connectionString }
        };

        var snapshot = new Mock<IOptionsSnapshot<BaGetterOptions>>();
        snapshot.Setup(s => s.Value).Returns(bagetterOptions);

        var services = new ServiceCollection();
        services.AddSingleton(snapshot.Object);
        services.AddDbContext<SqliteContext>(opts => opts.UseSqlite(_connectionString));

        return services.BuildServiceProvider();
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
}
