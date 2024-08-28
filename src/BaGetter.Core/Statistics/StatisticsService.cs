using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter.Core.Statistics;

public class StatisticsService : IStatisticsService
{
    private readonly IServiceProvider _serviceProvider;

    public StatisticsService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<int> GetPackagesTotalAmount()
    {
        var (scope, dbContext) = GetDbContext();
        var packagesCount = await dbContext.Packages.GroupBy(p => p.Id).CountAsync();
        scope.Dispose();
        return packagesCount;
    }

    public async Task<int> GetVersionsTotalAmount()
    {
        var (scope, dbContext) = GetDbContext();
        var packagesVersionsCount = await dbContext.Packages.CountAsync();
        scope.Dispose();
        return packagesVersionsCount;
    }

    public IEnumerable<string> GetKnownServices()
    {
        using var newScope = _serviceProvider.CreateScope();
        var configuration = newScope.ServiceProvider.GetRequiredService<IConfiguration>();
        var servicesNames = new List<string>();

        // Database providers.
        if (configuration.HasDatabaseType("MySql")) servicesNames.Add("MySql");
        if (configuration.HasDatabaseType("PostgreSql")) servicesNames.Add("PostgreSql");
        if (configuration.HasDatabaseType("SqlServer")) servicesNames.Add("SqlServer");
        if (configuration.HasDatabaseType("Sqlite")) servicesNames.Add("Sqlite");

        // Storage providers.
        if (configuration.HasStorageType("FileSystem")) servicesNames.Add("FileSystem");
        if (configuration.HasStorageType("AwsS3")) servicesNames.Add("AwsS3");
        if (configuration.HasStorageType("AliyunOss")) servicesNames.Add("AliyunOss");
        if (configuration.HasStorageType("GoogleCloud")) servicesNames.Add("GoogleCloud");
        if (configuration.HasStorageType("TencentCos")) servicesNames.Add("TencentCos");
        return servicesNames;
    }

    /// <summary>
    /// Creates a new DI scope and resolves an <see cref="IContext"/>.
    /// </summary>
    /// <remarks>Note, that the scope has to be disposed after the db queries.</remarks>
    /// <returns>A tuple of <see cref="IServiceScope"/> and <see cref="IContext"/>.</returns>
    private (IServiceScope, IContext) GetDbContext()
    {
        var newScope = _serviceProvider.CreateScope();
        var dbContext = newScope.ServiceProvider.GetRequiredService<IContext>();

        return (newScope, dbContext);
    }
}
