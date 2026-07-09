using System.Threading.Tasks;
using BaGetter.Core.Feeds;
using Microsoft.AspNetCore.Http;

namespace BaGetter.Web.Middleware;

public class FeedResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public FeedResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IFeedService feedService, FeedContext feedContext)
    {
        var cancellationToken = context.RequestAborted;

        if (context.Request.Path.StartsWithSegments("/feeds", out var remaining))
        {
            // Extract the slug from the next path segment
            var remainingStr = remaining.Value ?? string.Empty;
            var slugEnd = remainingStr.IndexOf('/', 1);
            string slug;
            string afterSlug;

            if (slugEnd < 0)
            {
                // Path is exactly /feeds/{slug} with no trailing path
                slug = remainingStr.TrimStart('/');
                afterSlug = "/";
            }
            else
            {
                slug = remainingStr[1..slugEnd];
                afterSlug = remainingStr[slugEnd..];
            }

            var feed = await feedService.GetFeedBySlugAsync(slug, cancellationToken);
            if (feed == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            context.Request.PathBase = context.Request.PathBase.Add($"/feeds/{slug}");
            context.Request.Path = afterSlug;
            feedContext.Set(feed, isDefaultRoute: false);
        }
        else
        {
            var defaultFeed = await feedService.GetDefaultFeedAsync(cancellationToken);
            feedContext.Set(defaultFeed, isDefaultRoute: true);
        }

        await _next(context);
    }
}
