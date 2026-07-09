using System;
using BaGetter.Core;
using BaGetter.Core.Configuration;
using BaGetter.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter.Database.SqlServer;

public static class SqlServerApplicationExtensions
{
    public static BaGetterApplication AddSqlServerDatabase(this BaGetterApplication app)
    {
        app.Services.AddBaGetDbContextProvider<SqlServerContext>("SqlServer");

        return app;
    }

    public static BaGetterApplication AddSqlServerDatabase(
        this BaGetterApplication app,
        Action<DatabaseOptions> configure)
    {
        app.AddSqlServerDatabase();
        app.Services.Configure(configure);
        return app;
    }
}
