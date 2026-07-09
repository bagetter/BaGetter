using System;
using BaGetter.Aliyun;
using BaGetter.Aws;
using BaGetter.Azure;
using BaGetter.DataProtection;
using BaGetter.Core;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using BaGetter.Core.Extensions;
using BaGetter.Core.Search;
using BaGetter.Core.Storage;
using BaGetter.Database.MySql;
using BaGetter.Database.PostgreSql;
using BaGetter.Database.Sqlite;
using BaGetter.Database.SqlServer;
using BaGetter.Gcp;
using BaGetter.Tencent;
using BaGetter.Web;
using BaGetter.Web.Extensions;
using BaGetter.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using HealthCheckOptions = Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions;

namespace BaGetter;

public class Startup
{
    private IConfiguration Configuration { get; }
    private IWebHostEnvironment Environment { get; }

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureOptions<ValidateBaGetterOptions>();
        services.ConfigureOptions<ConfigureBaGetterServer>();

        services.AddBaGetterOptions<IISServerOptions>(nameof(IISServerOptions));
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

        services.AddCors();

        ConfigureDataProtection(services);
    }

    private void ConfigureDataProtection(IServiceCollection services)
    {
        // Persist the Data Protection key ring through BaGetter's storage abstraction so it survives
        // container restarts and new revisions, for every storage backend (FileSystem, Azure Blob,
        // S3, GCS, OSS, COS). Without persistence the key ring lives in the container's ephemeral
        // filesystem and is regenerated on every restart, invalidating all existing antiforgery and
        // auth cookies — form POSTs then fail with HTTP 400 until users clear their cookies.
        services
            .AddDataProtection()
            .SetApplicationName("BaGetter");

        services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(
            sp => new ConfigureStorageXmlRepository(sp));
    }

    private void ConfigureBaGetterApplication(BaGetterApplication app)
    {
        //Add base authentication and authorization
        app.AddNugetBasicHttpAuthentication();
        app.AddNugetBasicHttpAuthorization();

        // Add Entra ID (OIDC) authentication when configured
        app.AddEntraAuthentication(Configuration, Environment);

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

        // Liveness probe
        // registered before FeedResolutionMiddleware so it never resolves the DbContext or other services.
        app.UseHealthChecks("/livez", new HealthCheckOptions { Predicate = _ => false });

        app.UseMiddleware<FeedStaticFilePathMiddleware>();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseMiddleware<FeedResolutionMiddleware>();
        app.UseRouting();
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
}
