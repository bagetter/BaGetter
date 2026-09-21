using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using BaGetter.Authentication;
using BaGetter.Core;
using BaGetter.Core.Configuration;
using BaGetter.Core.Extensions;
using BaGetter.Tencent;
using BaGetter.Web;
using BaGetter.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using HealthCheckOptions = Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions;

namespace BaGetter;

public class Startup
{
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureOptions<ValidateBaGetterOptions>();
        services.ConfigureOptions<ConfigureBaGetterServer>();

        AddWindowsIisServerOptions(services);
        services.AddBaGetterWebApplication(ConfigureBaGetterApplication);

        // You can swap between implementations of subsystems like storage and search using BaGetter's configuration.
        // Each subsystem's implementation has a provider that reads the configuration to determine if it should be
        // activated. BaGetter will run through all its providers until it finds one that is active.
        services.AddScoped(DependencyInjectionExtensions.GetServiceFromProviders<IContext>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IStorageService>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IPackageDatabase>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchService>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchIndexer>);

        services.AddHealthChecks();
        services.AddHostedService<SearchReindexHostedService>();

        services.AddCors();
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        var securityHeaders = Configuration.GetSection(nameof(BaGetterOptions.SecurityHeaders)).Get<SecurityHeadersOptions>() ?? new SecurityHeadersOptions();
        if (securityHeaders.EnableHsts)
        {
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(securityHeaders.HstsMaxAgeDays);
                options.IncludeSubDomains = securityHeaders.HstsIncludeSubDomains;
                options.Preload = securityHeaders.HstsPreload;
            });
        }

        var rateLimitOptions = Configuration.GetSection(nameof(BaGetterOptions.RequestRateLimit)).Get<RequestRateLimitOptions>() ?? new RequestRateLimitOptions();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = static (context, _) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = "60";
                return ValueTask.CompletedTask;
            };

            if (!rateLimitOptions.Enabled)
            {
                return;
            }

            var window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds);
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var key = httpContext.User?.Identity?.IsAuthenticated == true
                    ? $"user:{httpContext.User.Identity?.Name ?? "authenticated"}"
                    : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.PermitLimit,
                    Window = window,
                    QueueLimit = rateLimitOptions.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true,
                });
            });
        });
    }

    private static void AddWindowsIisServerOptions(IServiceCollection services)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var optionsType = Type.GetType("Microsoft.AspNetCore.Builder.IISServerOptions, Microsoft.AspNetCore.Server.IIS");
        if (optionsType == null)
        {
            return;
        }

        var method = typeof(Startup)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == nameof(AddWindowsIisServerOptions) && m.IsGenericMethodDefinition)
            .MakeGenericMethod(optionsType);

        method.Invoke(null, new object[] { services });
    }

    private static void AddWindowsIisServerOptions<TOptions>(IServiceCollection services)
        where TOptions : class
    {
        services.AddSingleton<IConfigureOptions<TOptions>>(provider =>
        {
            var baGetterOptions = provider.GetRequiredService<IOptions<BaGetterOptions>>();
            return new ConfigureNamedOptions<TOptions>(
                Options.DefaultName,
                options =>
                {
                    var maxRequestBodySize = typeof(TOptions).GetProperty("MaxRequestBodySize");
                    maxRequestBodySize?.SetValue(
                        options,
                        (long)baGetterOptions.Value.MaxPackageSizeGiB * int.MaxValue / 2);
                });
        });

        services.AddBaGetterOptions<TOptions>("IISServerOptions");
    }

    private void ConfigureBaGetterApplication(BaGetterApplication app)
    {
        //Add base authentication and authorization
        app.AddNugetBasicHttpAuthentication();
        app.AddNugetBasicHttpAuthorization();

        // Add database providers.
        app.AddAzureTableDatabase();
        app.AddMySqlDatabase();
        app.AddPostgreSqlDatabase();
        app.AddSqliteDatabase();
        app.AddSqlServerDatabase();

        // Add storage providers.
        app.AddFileStorage();
        app.AddAliyunOssStorage();
        app.AddAwsS3Storage();
        app.AddAzureBlobStorage();
        app.AddGoogleCloudStorage();
        app.AddTencentOssStorage();

        // Add search providers.
        //app.AddAzureSearch();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var options = Configuration.Get<BaGetterOptions>();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseStatusCodePages();
        }

        app.UseForwardedHeaders();
        app.UsePathBase(options.PathBase);
        if (!env.IsDevelopment() && options.SecurityHeaders?.EnableHsts == true)
        {
            app.UseHsts();
        }

        app.UseSecurityHeadersMiddleware();
        if (options.Localization?.Enabled == true)
        {
            app.Use(async (context, next) =>
            {
                if (string.Equals(context.Request.Query["clearCulture"], "1", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Cookies.Delete(CookieRequestCultureProvider.DefaultCookieName);
                    await next();
                    return;
                }

                var requestedCulture = context.Request.Query["culture"].ToString();
                var requestedUiCulture = context.Request.Query["ui-culture"].ToString();
                var selectedCulture = string.IsNullOrWhiteSpace(requestedUiCulture) ? requestedCulture : requestedUiCulture;

                if (!string.IsNullOrWhiteSpace(selectedCulture))
                {
                    var normalizedCulture = NormalizeSupportedCulture(selectedCulture);
                    if (!string.IsNullOrWhiteSpace(normalizedCulture))
                    {
                        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedCulture));
                        context.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, cookieValue);
                    }
                }

                await next();
            });

            app.UseRequestLocalization(BuildRequestLocalizationOptions(options.Localization));
        }
        app.UseResponseCompression();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseRequestTelemetryMiddleware();
        app.UseAuthorization();

        app.UseCors(ConfigureBaGetterServer.CorsPolicy);

        app.UseOperationCancelledMiddleware();

        app.UseEndpoints(endpoints =>
        {
            var baget = new BaGetterEndpointBuilder();

            baget.MapEndpoints(endpoints);
        });

        app.UseHealthChecks(options.HealthCheck.Path,
            new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    await report.FormatAsJson(context.Response.Body, options.Statistics.ListConfiguredServices, options.HealthCheck.StatusPropertyName,
                        context.RequestAborted);
                },
                Predicate = check => check.IsConfigured(options)
            }
        );
    }

    private static RequestLocalizationOptions BuildRequestLocalizationOptions(LocalizationOptions options)
    {
        options ??= new LocalizationOptions();

        var supportedCultures = LocalizationOptions.SupportedCultures
            .Select(c => new CultureInfo(c))
            .ToList();

        var defaultCultureName = string.IsNullOrWhiteSpace(options.DefaultCulture)
            ? "en-US"
            : options.DefaultCulture;

        var defaultCulture = supportedCultures.FirstOrDefault(c =>
                                 string.Equals(c.Name, defaultCultureName, StringComparison.OrdinalIgnoreCase))
                             ?? supportedCultures[0];

        var requestLocalizationOptions = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(defaultCulture),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
            FallBackToParentCultures = true,
            FallBackToParentUICultures = true,
            ApplyCurrentCultureToResponseHeaders = true,
        };

        requestLocalizationOptions.RequestCultureProviders = new IRequestCultureProvider[]
        {
            new QueryStringRequestCultureProvider(),
            new CookieRequestCultureProvider(),
            new CustomRequestCultureProvider(context =>
            {
                var header = context.Request.Headers.AcceptLanguage.ToString();
                if (string.IsNullOrWhiteSpace(header))
                {
                    return Task.FromResult<ProviderCultureResult>(null);
                }

                var firstLanguage = header.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(static x => x.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim())
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(firstLanguage))
                {
                    return Task.FromResult<ProviderCultureResult>(null);
                }

                var normalizedCulture = NormalizeSupportedCulture(firstLanguage);
                return Task.FromResult(string.IsNullOrWhiteSpace(normalizedCulture)
                    ? null
                    : new ProviderCultureResult(normalizedCulture, normalizedCulture));
            }),
            new AcceptLanguageHeaderRequestCultureProvider(),
        };

        return requestLocalizationOptions;
    }

    private static string NormalizeSupportedCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return null;
        }

        if (culture.StartsWith("is", StringComparison.OrdinalIgnoreCase))
        {
            return "is-IS";
        }

        if (culture.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return "en-US";
        }

        if (culture.StartsWith("da", StringComparison.OrdinalIgnoreCase))
        {
            return "da-DK";
        }

        if (culture.StartsWith("sv", StringComparison.OrdinalIgnoreCase))
        {
            return "sv-SE";
        }

        if (culture.StartsWith("nb", StringComparison.OrdinalIgnoreCase) ||
            culture.StartsWith("nn", StringComparison.OrdinalIgnoreCase) ||
            culture.StartsWith("no", StringComparison.OrdinalIgnoreCase))
        {
            return "nb-NO";
        }

        if (culture.StartsWith("pl", StringComparison.OrdinalIgnoreCase))
        {
            return "pl-PL";
        }

        if (culture.StartsWith("fi", StringComparison.OrdinalIgnoreCase))
        {
            return "fi-FI";
        }

        if (culture.StartsWith("es", StringComparison.OrdinalIgnoreCase))
        {
            return "es-ES";
        }

        if (culture.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            return "pt-PT";
        }

        if (culture.StartsWith("it", StringComparison.OrdinalIgnoreCase))
        {
            return "it-IT";
        }

        if (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return "zh-CN";
        }

        if (culture.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
        {
            return "fr-FR";
        }

        return LocalizationOptions.SupportedCultures.Any(s =>
            string.Equals(s, culture, StringComparison.OrdinalIgnoreCase))
            ? culture
            : null;
    }
}
