using System;
using BaGetter.Core;
using BaGetter.Database.PostgreSql;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter;

public static class PostgreSqlApplicationExtensions
{
    public static BaGetterApplication AddPostgreSqlDatabase(this BaGetterApplication app)
    {
        app.Services.AddBaGetDbContextProvider<PostgreSqlContext>("PostgreSql");

        return app;
    }

    public static BaGetterApplication AddPostgreSqlDatabase(
        this BaGetterApplication app,
        Action<DatabaseOptions> configure)
    {
        app.AddPostgreSqlDatabase();
        app.Services.Configure(configure);
        return app;
    }
}
