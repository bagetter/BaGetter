using System;
using BaGetter.Core;
using BaGetter.Database.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BaGetter;

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
