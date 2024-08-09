using System;
using System.Text.Json.Serialization;
using BaGetter.Authentication;
using BaGetter.Core;
using BaGetter.Web;
using BaGetter.Web.Authentication;
using BaGetter.Web.Helper;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBaGetterWebApplication(
        this IServiceCollection services,
        Action<BaGetterApplication> configureAction)
    {
        services
            .AddRouting(options => options.LowercaseUrls = true)
            .AddControllers()
            .AddApplicationPart(typeof(PackageContentController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddRazorPages();

        services.AddHttpContextAccessor();
        services.AddTransient<IUrlGenerator, BaGetterUrlGenerator>();

        services.AddSingleton(ApplicationVersionHelper.GetVersion());

        var app = services.AddBaGetterApplication(configureAction);
        app.AddNugetBasicHttpAuthentication();
        app.AddNugetBasicHttpAuthorization();

        return services;
    }

    private static BaGetterApplication AddNugetBasicHttpAuthentication(this BaGetterApplication app)
    {
        app.Services.AddAuthentication(options =>
        {
            // Breaks existing tests if the contains check is not here.
            if (!options.SchemeMap.ContainsKey(AuthenticationConstants.NugetBasicAuthenticationScheme))
            {
                options.AddScheme<NugetBasicAuthenticationHandler>(AuthenticationConstants.NugetBasicAuthenticationScheme, AuthenticationConstants.NugetBasicAuthenticationScheme);
                options.DefaultAuthenticateScheme = AuthenticationConstants.NugetBasicAuthenticationScheme;
                options.DefaultChallengeScheme = AuthenticationConstants.NugetBasicAuthenticationScheme;
            }
        });

        return app;
    }

    private static BaGetterApplication AddNugetBasicHttpAuthorization(this BaGetterApplication app)
    {
        app.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthenticationConstants.NugetUserPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
            });
        });

        return app;
    }
}
