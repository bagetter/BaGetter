using System;
using System.Collections.Generic;
using BaGetter.Core.Entities;
using BaGetter.Database.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BaGetter.Tests;

public class HostIntegrationTests
{
    private readonly string _databaseTypeKey = "Database:Type";
    private readonly string _connectionStringKey = "Database:ConnectionString";

    [Fact]
    public void ThrowsIfDatabaseTypeInvalid()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string>
        {
            { _databaseTypeKey, "InvalidType" }
        });

        using var scope = provider.CreateScope();
        Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<IContext>());
    }

    [Fact]
    public void ReturnsDatabaseContext()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string>
        {
            { _databaseTypeKey, "Sqlite" },
            { _connectionStringKey, "..." }
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IContext>());
    }

    [Fact]
    public void ReturnsSqliteContext()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string>
        {
            { _databaseTypeKey, "Sqlite" },
            { _connectionStringKey, "..." }
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SqliteContext>());
    }

    [Fact]
    public void DefaultsToSqlite()
    {
        var provider = BuildServiceProvider();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();

        Assert.IsType<SqliteContext>(context);
    }

    private IServiceProvider BuildServiceProvider(Dictionary<string, string> configs = null)
    {
        var host = Program
            .CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddInMemoryCollection(configs ?? new Dictionary<string, string>());
            })
            .Build();

        return host.Services;
    }
}
