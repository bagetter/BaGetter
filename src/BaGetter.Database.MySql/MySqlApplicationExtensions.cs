using System;
using BaGetter.Core;
using BaGetter.Core.Configuration;
using BaGetter.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter.Database.MySql;

public static class MySqlApplicationExtensions
{
    public static BaGetterApplication AddMySqlDatabase(this BaGetterApplication app)
    {
        app.Services.AddBaGetDbContextProvider<MySqlContext>("MySql");

        return app;
    }

    public static BaGetterApplication AddMySqlDatabase(
        this BaGetterApplication app,
        Action<DatabaseOptions> configure)
    {
        app.AddMySqlDatabase();
        app.Services.Configure(configure);
        return app;
    }
}
