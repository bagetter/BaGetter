using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

        app.Command("reindex", reindex =>
        {
            reindex.Command("search", search =>
            {
                search.OnExecuteAsync(async cancellationToken =>
                {
                    using var scope = host.Services.CreateScope();
                    var reindexService = scope.ServiceProvider.GetRequiredService<ISearchReindexService>();

                    await reindexService.ReindexAsync(cancellationToken);
                });
            });
        });

        app.Command("hash", hash =>
        {
            var valueArgument = hash.Argument("value", "The secret value to hash.");
            var iterationsOption = hash.Option("--iterations <N>", "PBKDF2 iteration count (default: 100000).", CommandOptionType.SingleValue);

            hash.OnExecute(() =>
            {
                if (string.IsNullOrWhiteSpace(valueArgument.Value))
                {
                    Console.Error.WriteLine("You must provide a value to hash.");
                    hash.ShowHelp();
                    return 1;
                }

                var iterations = 100_000;
                if (iterationsOption.HasValue() &&
                    (!int.TryParse(iterationsOption.Value(), out iterations) || iterations < 10_000))
                {
                    Console.Error.WriteLine("Iterations must be an integer >= 10000.");
                    return 1;
                }

                Console.WriteLine(SecretHashing.HashSecret(valueArgument.Value, iterations));
                return 0;
            });
        });

        app.Command("install", install =>
        {
            install.Command("service", service =>
            {
                var nameOption = service.Option("--name <NAME>", "Windows service name.", CommandOptionType.SingleValue);
                var displayNameOption = service.Option("--display-name <DISPLAY_NAME>", "Windows service display name.", CommandOptionType.SingleValue);
                var urlsOption = service.Option("--urls <URLS>", "Kestrel URL bindings for the service.", CommandOptionType.SingleValue);
                var startOption = service.Option("--start", "Start service immediately after install.", CommandOptionType.NoValue);

                service.OnExecuteAsync(async cancellationToken =>
                {
                    await Task.CompletedTask;

                    if (!OperatingSystem.IsWindows())
                    {
                        Console.Error.WriteLine("The 'install service' command is only supported on Windows.");
                        service.ShowHelp();
                        return 1;
                    }

                    var serviceName = nameOption.HasValue() ? nameOption.Value() : "BaGetter";
                    var displayName = displayNameOption.HasValue() ? displayNameOption.Value() : serviceName;
                    var configuredUrls = host.Services.GetRequiredService<IConfiguration>()["Urls"]
                        ?? host.Services.GetRequiredService<IConfiguration>()["urls"];
                    var urls = urlsOption.HasValue()
                        ? urlsOption.Value()
                        : (!string.IsNullOrWhiteSpace(configuredUrls) ? configuredUrls : "http://0.0.0.0:50561");

                    var binPath = BuildWindowsServiceBinPath(urls);

                    var createResult = RunCommand(
                        "sc.exe",
                        $"create \"{serviceName}\" binPath= \"{binPath}\" start= auto DisplayName= \"{displayName}\"");

                    if (createResult != 0)
                    {
                        Console.Error.WriteLine($"Failed to create service '{serviceName}'. Ensure you run as Administrator.");
                        return createResult;
                    }

                    RunCommand("sc.exe", $"description \"{serviceName}\" \"BaGetter NuGet server\"");

                    if (startOption.HasValue())
                    {
                        var startResult = RunCommand("sc.exe", $"start \"{serviceName}\"");
                        if (startResult != 0)
                        {
                            Console.Error.WriteLine($"Service '{serviceName}' created but failed to start.");
                            return startResult;
                        }
                    }

                    Console.WriteLine($"Service '{serviceName}' installed successfully.");
                    return 0;
                });
            });
        });

        app.Command("uninstall", uninstall =>
        {
            uninstall.Command("service", service =>
            {
                var nameOption = service.Option("--name <NAME>", "Windows service name.", CommandOptionType.SingleValue);
                var stopOption = service.Option("--stop", "Stop service before uninstall.", CommandOptionType.NoValue);

                service.OnExecuteAsync(async cancellationToken =>
                {
                    await Task.CompletedTask;

                    if (!OperatingSystem.IsWindows())
                    {
                        Console.Error.WriteLine("The 'uninstall service' command is only supported on Windows.");
                        service.ShowHelp();
                        return 1;
                    }

                    var serviceName = nameOption.HasValue() ? nameOption.Value() : "BaGetter";

                    if (stopOption.HasValue())
                    {
                        RunCommand("sc.exe", $"stop \"{serviceName}\"");
                    }

                    var deleteResult = RunCommand("sc.exe", $"delete \"{serviceName}\"");
                    if (deleteResult != 0)
                    {
                        Console.Error.WriteLine($"Failed to uninstall service '{serviceName}'. Ensure the service exists and you run as Administrator.");
                        return deleteResult;
                    }

                    Console.WriteLine($"Service '{serviceName}' uninstalled successfully.");
                    return 0;
                });
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
        var builder = Host
            .CreateDefaultBuilder(args);

        builder = TryConfigureWindowsService(builder);

        return builder
            .ConfigureAppConfiguration((ctx, config) =>
            {
                var root = Environment.GetEnvironmentVariable("BAGET_CONFIG_ROOT");

                if (!string.IsNullOrEmpty(root))
                {
                    config.SetBasePath(root);
                }
                // Cross-platform configuration paths
                var configDirectory = OperatingSystem.IsWindows()
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BaGetter")
                    : "/etc/BaGetter";

                var webSettingsPath = Path.Combine(configDirectory, "AppSettings.json");
                config.AddJsonFile(webSettingsPath, optional: true, reloadOnChange: true);

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

    private static IHostBuilder TryConfigureWindowsService(IHostBuilder builder)
    {
        if (!OperatingSystem.IsWindows())
        {
            return builder;
        }

        const string windowsServicesAssemblyName = "Microsoft.Extensions.Hosting.WindowsServices";
        try
        {
            var assembly = Assembly.Load(windowsServicesAssemblyName);
            var useWindowsServiceMethod = assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .FirstOrDefault(m =>
                {
                    if (!string.Equals(m.Name, "UseWindowsService", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    var parameters = m.GetParameters();
                    return parameters.Length > 0 && parameters[0].ParameterType == typeof(IHostBuilder);
                });

            if (useWindowsServiceMethod == null)
            {
                Console.Error.WriteLine(
                    "Windows Service integration method not found. Continuing without UseWindowsService().");
                return builder;
            }

            var parameters = useWindowsServiceMethod.GetParameters();
            var methodArgs = parameters.Length switch
            {
                1 => new object?[] { builder },
                2 => new object?[] { builder, CreateServiceNameConfigureDelegate(parameters[1].ParameterType) },
                _ => new object?[] { builder },
            };

            var result = useWindowsServiceMethod.Invoke(null, methodArgs);
            return result as IHostBuilder ?? builder;
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine(
                "Windows Service integration assembly not found. Continuing without UseWindowsService().");
            return builder;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Failed to enable Windows Service integration ({ex.GetType().Name}). Continuing without UseWindowsService().");
            return builder;
        }
    }

    private static object? CreateServiceNameConfigureDelegate(Type configureDelegateType)
    {
        if (!configureDelegateType.IsGenericType ||
            configureDelegateType.GetGenericTypeDefinition() != typeof(Action<>))
        {
            return null;
        }

        var optionsType = configureDelegateType.GetGenericArguments()[0];
        var method = typeof(Program).GetMethod(
            nameof(ConfigureWindowsServiceOptions),
            BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
        {
            return null;
        }

        var genericMethod = method.MakeGenericMethod(optionsType);
        return Delegate.CreateDelegate(configureDelegateType, genericMethod);
    }

    private static void ConfigureWindowsServiceOptions<TOptions>(TOptions options)
    {
        var serviceNameProperty = typeof(TOptions).GetProperty("ServiceName", BindingFlags.Public | BindingFlags.Instance);
        if (serviceNameProperty?.CanWrite == true && serviceNameProperty.PropertyType == typeof(string))
        {
            serviceNameProperty.SetValue(options, "BaGetter");
        }
    }

    private static string BuildWindowsServiceBinPath(string urls)
    {
        var processPath = Environment.ProcessPath ?? string.Empty;
        var entryAssembly = Assembly.GetEntryAssembly()?.Location;

        if (processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(entryAssembly))
        {
            return $"\"{processPath}\" \"{entryAssembly}\" --urls \"{urls}\"";
        }

        return $"\"{processPath}\" --urls \"{urls}\"";
    }

    private static int RunCommand(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            Console.Error.WriteLine($"Failed to start command '{fileName} {arguments}'.");
            return 1;
        }

        process.WaitForExit();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            Console.WriteLine(stdout.TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Console.Error.WriteLine(stderr.TrimEnd());
        }

        return process.ExitCode;
    }
}
