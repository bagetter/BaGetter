using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BaGetter.Core;
using BaGetter.Web;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        app.Command("deprecate", cmd =>
        {
            cmd.Description = "Deprecate a package on a BaGetter feed";

            var idArg = cmd.Argument("packageId", "Package id to deprecate").IsRequired();
            var versionArg = cmd.Argument("version", "Package version to deprecate").IsRequired();
            var reasonsOpt = cmd.Option("-r|--reasons", "Comma-separated deprecation reasons (Legacy,CriticalBugs,Other)", CommandOptionType.SingleValue).IsRequired();
            var messageOpt = cmd.Option("-m|--message", "Optional deprecation message", CommandOptionType.SingleValue);
            var altIdOpt = cmd.Option("-a|--alternate-package", "Alternate package id", CommandOptionType.SingleValue);
            var altVersionOpt = cmd.Option("-v|--alternate-version", "Alternate package version or range", CommandOptionType.SingleValue);
            var sourceOpt = cmd.Option("-s|--source", "Feed base URL (defaults to http://localhost:5000)", CommandOptionType.SingleValue);
            var apiKeyOpt = cmd.Option("-k|--api-key", "API key with push permissions", CommandOptionType.SingleValue).IsRequired();

            cmd.OnExecuteAsync(async cancellationToken =>
            {
                var reasons = (reasonsOpt.Value() ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .ToArray();

                var payload = new DeprecatePackageRequest
                {
                    Reasons = reasons,
                    Message = messageOpt.Value(),
                    AlternatePackageId = altIdOpt.Value(),
                    AlternatePackageVersion = altVersionOpt.Value()
                };

                var source = sourceOpt.HasValue()
                    ? sourceOpt.Value().TrimEnd('/')
                    : "http://localhost:5000";

                var endpoint = $"{source}/api/v2/package/{idArg.Value}/{versionArg.Value}/deprecate";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-NuGet-ApiKey", apiKeyOpt.Value());

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var response = await client.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Deprecated {idArg.Value} {versionArg.Value}.");
                    return;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Failed to deprecate package ({(int)response.StatusCode}): {body}");
                Environment.ExitCode = 1;
            });
        });

        app.Option("--urls", "The URLs that BaGetter should bind to.", CommandOptionType.SingleValue);

        app.OnExecuteAsync(async cancellationToken =>
        {
            await host.RunMigrationsAsync(cancellationToken);
            await host.RunAsync(cancellationToken);
        });

        await app.ExecuteAsync(args);
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
