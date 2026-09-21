using BaGetter.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace BaGetter;

public class ConfigureBaGetterServer
    : IConfigureOptions<CorsOptions>
    , IConfigureOptions<FormOptions>
    , IConfigureOptions<ForwardedHeadersOptions>
{
    public const string CorsPolicy = "AllowAll";
    private readonly BaGetterOptions _baGetterOptions;

    public ConfigureBaGetterServer(IOptions<BaGetterOptions> baGetterOptions)
    {
        _baGetterOptions = baGetterOptions.Value;
    }


    public void Configure(CorsOptions options)
    {
        var cors = _baGetterOptions.Cors ?? new CorsPolicyOptions();
        options.AddPolicy(
            CorsPolicy,
            builder =>
            {
                if (cors.AllowAnyOrigin || cors.AllowedOrigins.Length == 0)
                {
                    builder.AllowAnyOrigin();
                }
                else
                {
                    builder.WithOrigins(cors.AllowedOrigins);
                    if (cors.AllowCredentials)
                    {
                        builder.AllowCredentials();
                    }
                }

                if (cors.AllowAnyMethod || cors.AllowedMethods.Length == 0)
                {
                    builder.AllowAnyMethod();
                }
                else
                {
                    builder.WithMethods(cors.AllowedMethods);
                }

                if (cors.AllowAnyHeader || cors.AllowedHeaders.Length == 0)
                {
                    builder.AllowAnyHeader();
                }
                else
                {
                    builder.WithHeaders(cors.AllowedHeaders);
                }
            });
    }

    public void Configure(FormOptions options)
    {
        // Allow packages up to ~8GiB in size
        options.MultipartBodyLengthLimit = (long) _baGetterOptions.MaxPackageSizeGiB * int.MaxValue / 2;
    }

    public void Configure(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

        // Do not restrict to local network/proxy
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }

}
