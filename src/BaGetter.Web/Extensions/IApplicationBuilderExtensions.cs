using System;
using Microsoft.AspNetCore.Builder;

namespace BaGetter.Web;

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseOperationCancelledMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<OperationCancelledMiddleware>();
    }

    public static IApplicationBuilder UseRequestTelemetryMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RequestTelemetryMiddleware>();
    }

    public static IApplicationBuilder UseSecurityHeadersMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
