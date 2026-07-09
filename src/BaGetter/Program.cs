using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using BaGetter.Core.Feeds;
using BaGetter.Core.Upstream;
using BaGetter.Web.Extensions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        if (!host.ValidateStartupOptions())
        {
            return;
        }

        var app = new CommandLineApplication
        {
            Name = "baget",
            Description = "A light-weight NuGet service",
        };

        app.HelpOption(inherited: true);

        app.Command("import", import =>
        {
            import.Command("downloads", downloads =>
            {
                downloads.OnExecuteAsync(async cancellationToken =>
                {
                    using var scope = host.Services.CreateScope();
                    var importer = scope.ServiceProvider.GetRequiredService<DownloadsImporter>();

                    await importer.ImportAsync(cancellationToken);
                });
            });
        });

        app.Option("--urls", "The URLs that BaGetter should bind to.", CommandOptionType.SingleValue);

        app.OnExecuteAsync(async cancellationToken =>
        {
            await host.RunMigrationsAsync(cancellationToken);

            using (var scope = host.Services.CreateScope())
            {
                var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();
                await feedService.EnsureDefaultFeedExistsAsync(cancellationToken);
                await MigrateGlobalMirrorConfigToDefaultFeedAsync(scope.ServiceProvider, cancellationToken);
            }

            await host.RunAsync(cancellationToken);
        });

        await app.ExecuteAsync(args);
    }

    /// <summary>
    /// One-time upgrade helper: if the global Mirror config has Enabled=true and the default
    /// feed has not yet had mirror settings populated, copy them. The guard checks for a
    /// populated mirror source on the feed rather than <c>MirrorEnabled</c>, so an admin who
    /// later toggles mirroring off on the default feed won't have it silently re-enabled on
    /// the next startup.
    /// </summary>
    private static async Task MigrateGlobalMirrorConfigToDefaultFeedAsync(
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var mirrorOptions = provider.GetRequiredService<IOptions<MirrorOptions>>().Value;

        if (!mirrorOptions.Enabled || mirrorOptions.PackageSource == null)
            return;

        var feedService = provider.GetRequiredService<IFeedService>();
        var logger = provider.GetRequiredService<ILogger<Program>>();

        var defaultFeed = await feedService.GetDefaultFeedAsync(cancellationToken);
        if (defaultFeed == null)
        {
            logger.LogWarning("Default feed not found during mirror config migration; skipping.");
            return;
        }

        // Guard: already migrated. Detect based on whether any mirror fields have been
        // populated on the default feed, not on MirrorEnabled alone — otherwise an admin
        // who intentionally disabled mirroring would see it re-enabled on every startup
        // as long as the obsolete global Mirror config still exists in appsettings.
        if (!string.IsNullOrWhiteSpace(defaultFeed.MirrorPackageSource))
        {
            logger.LogDebug(
                "Default feed already has mirror settings; skipping migration.");
            return;
        }

        logger.LogInformation(
            "Copying global Mirror configuration to default feed (one-time upgrade).");

        defaultFeed.MirrorEnabled = true;
        defaultFeed.MirrorPackageSource = mirrorOptions.PackageSource.ToString();
        defaultFeed.MirrorLegacy = mirrorOptions.Legacy;
        defaultFeed.MirrorDownloadTimeoutSeconds = mirrorOptions.PackageDownloadTimeoutSeconds;

        if (mirrorOptions.Authentication is { Type: not MirrorAuthenticationType.None } auth)
        {
            defaultFeed.MirrorAuthType = auth.Type;
            defaultFeed.MirrorAuthUsername = auth.Username;
            defaultFeed.MirrorAuthPassword = auth.Password;
            defaultFeed.MirrorAuthToken = auth.Token;

            if (auth.CustomHeaders is { Count: > 0 })
            {
                defaultFeed.MirrorAuthCustomHeaders =
                    JsonSerializer.Serialize(auth.CustomHeaders);
            }
        }

        await feedService.UpdateFeedAsync(defaultFeed, cancellationToken);

        logger.LogInformation(
            "Global Mirror configuration copied to default feed (source: {PackageSource}).",
            defaultFeed.MirrorPackageSource);
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, config) =>
            {
                var root = Environment.GetEnvironmentVariable("BAGET_CONFIG_ROOT");

                if (!string.IsNullOrEmpty(root))
                {
                    config.SetBasePath(root);
                }

                // Optionally load secrets from files in the conventional path
                config.AddKeyPerFile("/run/secrets", optional: true);
            })
            .ConfigureWebHostDefaults(web =>
            {
                web.ConfigureKestrel(options =>
                {
                    // Remove the upload limit from Kestrel. If needed, an upload limit can
                    // be enforced by a reverse proxy server, like IIS.
                    options.Limits.MaxRequestBodySize = null;
                });

                web.UseStartup<Startup>();
            });
    }
}
