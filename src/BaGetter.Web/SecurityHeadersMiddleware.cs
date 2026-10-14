using System;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BaGetter.Web;

internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<BaGetterOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value.SecurityHeaders ?? new SecurityHeadersOptions();
    }

    public async Task Invoke(HttpContext context)
    {
        if (_options.Enabled)
        {
            context.Response.OnStarting(static state =>
            {
                var response = (HttpResponse)state;
                response.Headers["X-Content-Type-Options"] = "nosniff";
                response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                response.Headers["Referrer-Policy"] = "no-referrer";
                response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
                response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                return Task.CompletedTask;
            }, context.Response);
        }

        await _next(context);
    }
}
