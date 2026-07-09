using System;
using System.Text.Json.Serialization;
using BaGetter.Core;
using BaGetter.Core.Extensions;
using BaGetter.Web.Controllers;
using BaGetter.Web.Helper;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter.Web.Extensions;

public static class ServiceCollectionExtensions
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

        return services;
    }
}
