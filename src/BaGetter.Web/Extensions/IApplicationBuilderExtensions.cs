using System;
using Microsoft.AspNetCore.Builder;

namespace BaGetter.Web.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseOperationCancelledMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<OperationCancelledMiddleware>();
    }
}
