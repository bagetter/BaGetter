using System;
using BaGetter.Core;
using BaGetter.Database.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter;

public static class SqliteApplicationExtensions
{
    public static BaGetterApplication AddSqliteDatabase(this BaGetterApplication app)
    {
        app.Services.AddBaGetDbContextProvider<SqliteContext>("Sqlite");

        return app;
    }

    public static BaGetterApplication AddSqliteDatabase(
        this BaGetterApplication app,
        Action<DatabaseOptions> configure)
    {
        app.AddSqliteDatabase();
        app.Services.Configure(configure);
        return app;
    }
}
