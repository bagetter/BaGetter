using BaGetter.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BaGetter.Tencent;

public static class TencentApplicationExtensions
{
    public static BaGetterApplication AddTencentOssStorage(this BaGetterApplication app)
    {
        app.Services.AddBaGetterOptions<TencentStorageOptions>(nameof(BaGetterOptions.Storage));

        app.Services.AddTransient<TencentStorageService>();
        app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<TencentStorageService>());

        app.Services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TencentStorageOptions>>().Value;

            return new TencentCosClient(options);
        });

        app.Services.AddProvider<IStorageService>((provider, config) =>
        {
            if (!config.HasStorageType("TencentCos"))
                return null;

            return provider.GetRequiredService<TencentStorageService>();
        });

        return app;
    }

    public static BaGetterApplication AddTencentOssStorage(
        this BaGetterApplication app,
        Action<TencentStorageOptions> configure)
    {
        app.AddTencentOssStorage();
        app.Services.Configure(configure);
        return app;
    }
}
