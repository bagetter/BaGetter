using System;
using BaGetter.Core.Configuration;
using BaGetter.Core.Search;
using BaGetter.Core.Statistics;
using BaGetter.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BaGetter.Core.Extensions;

public static class BaGetterApplicationExtensions
{
    public static BaGetterApplication AddFileStorage(this BaGetterApplication app)
    {
        app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<FileStorageService>());

        return app;
    }

    public static BaGetterApplication AddFileStorage(
        this BaGetterApplication app,
        Action<FileSystemStorageOptions> configure)
    {
        app.AddFileStorage();
        app.Services.Configure(configure);
        return app;
    }

    public static BaGetterApplication AddNullStorage(this BaGetterApplication app)
    {
        app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<NullStorageService>());
        return app;
    }

    public static BaGetterApplication AddNullSearch(this BaGetterApplication app)
    {
        app.Services.TryAddTransient<ISearchIndexer>(provider => provider.GetRequiredService<NullSearchIndexer>());
        app.Services.TryAddTransient<ISearchService>(provider => provider.GetRequiredService<NullSearchService>());
        return app;
    }

    public static BaGetterApplication AddStatistics(this BaGetterApplication app)
    {
        app.Services.TryAddSingleton<IStatisticsService, StatisticsService>();
        return app;
    }
}
