using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Feeds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BaGetter.Web.Authentication;

public class FeedPermissionHandler : AuthorizationHandler<FeedPermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly IUserService _userService;
    private readonly IFeedContext _feedContext;
    private readonly IOptions<BaGetterOptions> _options;

    public FeedPermissionHandler(
        IPermissionService permissionService,
        IUserService userService,
        IFeedContext feedContext,
        IOptions<BaGetterOptions> options)
    {
        _permissionService = permissionService;
        _userService = userService;
        _feedContext = feedContext;
        _options = options;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FeedPermissionRequirement requirement)
    {
        var authMode = _options.Value.Authentication?.Mode ?? AuthenticationMode.Config;

        // In static auth mode, the existing auth handler already did its job
        if (authMode == AuthenticationMode.Config)
        {
            context.Succeed(requirement);
            return;
        }

        // Anonymous users get a pass if they have the Anonymous claim (static auth mode fallback)
        if (context.User.HasClaim(c => c.Type == ClaimTypes.Anonymous))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Fail();
            return;
        }

        // Fail closed if feed context is unavailable
        if (_feedContext.CurrentFeed == null)
        {
            context.Fail();
            return;
        }

        var feedId = _feedContext.CurrentFeed.Id;

        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;

        var hasPermission = requirement.Permission switch
        {
            FeedPermissionRequirement.Pull => await _permissionService.CanPullAsync(userId, feedId, cancellationToken),
            FeedPermissionRequirement.Push => await _permissionService.CanPushAsync(userId, feedId, cancellationToken),
            FeedPermissionRequirement.Delete => await _permissionService.CanDeleteAsync(userId, feedId, cancellationToken),
            FeedPermissionRequirement.Admin => await _userService.IsAdminAsync(userId, cancellationToken),
            _ => false
        };

        if (hasPermission)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
