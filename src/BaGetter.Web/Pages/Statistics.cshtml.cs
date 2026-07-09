using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Feeds;
using BaGetter.Core.Statistics;
using BaGetter.Web.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BaGetter.Web.Pages;

public class StatisticsModel : PageModel
{
    private readonly IOptionsSnapshot<StatisticsOptions> _options;
    private readonly IFeedContext _feedContext;
    private readonly IStatisticsService _statisticsService;
    private readonly IPermissionService _permissions;
    private readonly IOptionsSnapshot<NugetAuthenticationOptions> _authOptions;

    public StatisticsModel(
        IOptionsSnapshot<StatisticsOptions> options,
        IFeedContext feedContext,
        IStatisticsService statisticsService,
        IPermissionService permissions,
        IOptionsSnapshot<NugetAuthenticationOptions> authOptions)
    {
        _options = options;
        _feedContext = feedContext ?? throw new ArgumentNullException(nameof(feedContext));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _authOptions = authOptions ?? throw new ArgumentNullException(nameof(authOptions));
    }

    public string FeedName { get; private set; }
    public int PackagesTotal { get; private set; }
    public int VersionsTotal { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.EnableStatisticsPage) return NotFound();

        var denied = await FeedAccessGuard.CheckReadAccessAsync(
            HttpContext, _feedContext, _permissions, _authOptions.Value.Mode, cancellationToken);
        if (denied != null) return denied;

        var feed = _feedContext.CurrentFeed;
        FeedName = feed.Name;
        PackagesTotal = await _statisticsService.GetPackagesTotalAmount(feed.Id);
        VersionsTotal = await _statisticsService.GetVersionsTotalAmount(feed.Id);
        return Page();
    }
}
