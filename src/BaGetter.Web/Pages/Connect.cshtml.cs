using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Feeds;
using BaGetter.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BaGetter.Web.Pages;

public class ConnectModel : PageModel
{
    private readonly IPermissionService _permissions;
    private readonly IFeedContext _feedContext;
    private readonly IOptionsSnapshot<NugetAuthenticationOptions> _authOptions;

    public ConnectModel(
        IPermissionService permissions,
        IFeedContext feedContext,
        IOptionsSnapshot<NugetAuthenticationOptions> authOptions)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _feedContext = feedContext ?? throw new ArgumentNullException(nameof(feedContext));
        _authOptions = authOptions ?? throw new ArgumentNullException(nameof(authOptions));
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var denied = await FeedAccessGuard.CheckReadAccessAsync(
            HttpContext, _feedContext, _permissions, _authOptions.Value.Mode, cancellationToken);
        if (denied != null) return denied;

        return Page();
    }
}
