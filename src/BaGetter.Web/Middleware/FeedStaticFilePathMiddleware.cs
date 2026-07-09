using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BaGetter.Web.Middleware;

/// <summary>
/// Strips the /feeds/{slug} prefix from _content/ requests so that
/// UseStaticFiles can resolve Blazor/Razor static web assets even when
/// the browser constructs the URL relative to the feed PathBase.
/// </summary>
public class FeedStaticFilePathMiddleware
{
    private readonly RequestDelegate _next;

    public FeedStaticFilePathMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/feeds/", StringComparison.OrdinalIgnoreCase))
        {
            var afterFeeds = path["/feeds/".Length..];
            var nextSlash = afterFeeds.IndexOf('/');
            if (nextSlash >= 0)
            {
                var afterSlug = afterFeeds[nextSlash..];
                if (afterSlug.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Path = afterSlug;
                }
            }
        }

        return _next(context);
    }
}
