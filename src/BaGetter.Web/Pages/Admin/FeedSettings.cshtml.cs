using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BaGetter.Web.Pages.Admin;

[Authorize(AuthenticationSchemes = Core.Authentication.AuthenticationConstants.CookieScheme)]
public class FeedSettingsModel : PageModel
{
    private readonly IFeedService _feedService;
    private readonly IUserService _userService;

    public FeedSettingsModel(IFeedService feedService, IUserService userService, IOptions<BaGetterOptions> options)
    {
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        GlobalOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public BaGetterOptions GlobalOptions { get; }

    public Feed Feed { get; set; }

    // General
    [BindProperty]
    public string Name { get; set; }

    [BindProperty]
    public string Description { get; set; }

    [BindProperty]
    public bool? IsReadOnlyMode { get; set; }

    [BindProperty]
    public bool UseGlobalReadOnly { get; set; }

    // Package behavior
    [BindProperty]
    public PackageOverwriteAllowed? AllowPackageOverwrites { get; set; }

    [BindProperty]
    public bool UseGlobalOverwrite { get; set; }

    [BindProperty]
    public PackageDeletionBehavior? PackageDeletionBehavior { get; set; }

    [BindProperty]
    public bool UseGlobalDeletion { get; set; }

    [BindProperty]
    public uint? MaxPackageSizeGiB { get; set; }

    [BindProperty]
    public bool UseGlobalMaxSize { get; set; }

    // Retention
    [BindProperty]
    public int? RetentionMaxMajorVersions { get; set; }

    [BindProperty]
    public bool UseGlobalRetentionMajor { get; set; }

    [BindProperty]
    public int? RetentionMaxMinorVersions { get; set; }

    [BindProperty]
    public bool UseGlobalRetentionMinor { get; set; }

    [BindProperty]
    public int? RetentionMaxPatchVersions { get; set; }

    [BindProperty]
    public bool UseGlobalRetentionPatch { get; set; }

    [BindProperty]
    public int? RetentionMaxPrereleaseVersions { get; set; }

    [BindProperty]
    public bool UseGlobalRetentionPrerelease { get; set; }

    // Mirror
    [BindProperty]
    public bool MirrorEnabled { get; set; }

    [BindProperty]
    public string MirrorPackageSource { get; set; }

    [BindProperty]
    public bool MirrorLegacy { get; set; }

    [BindProperty]
    public int? MirrorDownloadTimeoutSeconds { get; set; }

    [BindProperty]
    public MirrorAuthenticationType? MirrorAuthType { get; set; }

    [BindProperty]
    public string MirrorAuthUsername { get; set; }

    // Not bound from form — only set on explicit save; use "leave blank to keep" pattern
    [BindProperty]
    public string MirrorAuthPasswordNew { get; set; }

    [BindProperty]
    public string MirrorAuthTokenNew { get; set; }

    [BindProperty]
    public string MirrorAuthCustomHeaders { get; set; }

    public string SuccessMessage { get; set; }
    public string ErrorMessage { get; set; }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    private async Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return false;
        return await _userService.IsAdminAsync(userId, cancellationToken);
    }

    private void PopulateFromFeed(Feed feed)
    {
        Name = feed.Name;
        Description = feed.Description;

        UseGlobalReadOnly = !feed.IsReadOnlyMode.HasValue;
        IsReadOnlyMode = feed.IsReadOnlyMode;

        UseGlobalOverwrite = !feed.AllowPackageOverwrites.HasValue;
        AllowPackageOverwrites = feed.AllowPackageOverwrites;

        UseGlobalDeletion = !feed.PackageDeletionBehavior.HasValue;
        PackageDeletionBehavior = feed.PackageDeletionBehavior;

        UseGlobalMaxSize = !feed.MaxPackageSizeGiB.HasValue;
        MaxPackageSizeGiB = feed.MaxPackageSizeGiB;

        UseGlobalRetentionMajor = !feed.RetentionMaxMajorVersions.HasValue;
        RetentionMaxMajorVersions = feed.RetentionMaxMajorVersions;

        UseGlobalRetentionMinor = !feed.RetentionMaxMinorVersions.HasValue;
        RetentionMaxMinorVersions = feed.RetentionMaxMinorVersions;

        UseGlobalRetentionPatch = !feed.RetentionMaxPatchVersions.HasValue;
        RetentionMaxPatchVersions = feed.RetentionMaxPatchVersions;

        UseGlobalRetentionPrerelease = !feed.RetentionMaxPrereleaseVersions.HasValue;
        RetentionMaxPrereleaseVersions = feed.RetentionMaxPrereleaseVersions;

        MirrorEnabled = feed.MirrorEnabled;
        MirrorPackageSource = feed.MirrorPackageSource;
        MirrorLegacy = feed.MirrorLegacy;
        MirrorDownloadTimeoutSeconds = feed.MirrorDownloadTimeoutSeconds;
        MirrorAuthType = feed.MirrorAuthType;
        MirrorAuthUsername = feed.MirrorAuthUsername;
        MirrorAuthCustomHeaders = feed.MirrorAuthCustomHeaders;
        // Secrets (Password/Token) are NOT populated — use "leave blank to keep" pattern
    }

    private static readonly HashSet<string> _blockedHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Cookie", "Host", "Content-Length", "Transfer-Encoding",
        "Connection", "Upgrade", "Proxy-Authorization", "Set-Cookie"
    };

    private static bool TryValidateCustomHeaders(string json, out string error)
    {
        Dictionary<string, string> headers;
        try
        {
            headers = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (JsonException)
        {
            error = "Custom headers must be valid JSON (e.g. {\"X-My-Header\": \"value\"}).";
            return false;
        }

        if (headers == null)
        {
            error = "Custom headers must be a JSON object.";
            return false;
        }

        foreach (var name in headers.Keys)
        {
            if (_blockedHeaderNames.Contains(name))
            {
                error = $"Custom headers may not include the security-sensitive header '{name}'.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        Feed = await _feedService.GetFeedBySlugAsync(slug, cancellationToken);
        if (Feed == null) return NotFound();

        PopulateFromFeed(Feed);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        Feed = await _feedService.GetFeedBySlugAsync(slug, cancellationToken);
        if (Feed == null) return NotFound();

        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 256)
        {
            ErrorMessage = "Name is required and must be at most 256 characters.";
            PopulateFromFeed(Feed);
            return Page();
        }

        Feed.Name = Name.Trim();
        Feed.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();

        Feed.IsReadOnlyMode = UseGlobalReadOnly ? null : IsReadOnlyMode;
        Feed.AllowPackageOverwrites = UseGlobalOverwrite ? null : AllowPackageOverwrites;
        Feed.PackageDeletionBehavior = UseGlobalDeletion ? null : PackageDeletionBehavior;
        Feed.MaxPackageSizeGiB = UseGlobalMaxSize ? null : MaxPackageSizeGiB;

        Feed.RetentionMaxMajorVersions = UseGlobalRetentionMajor ? null : RetentionMaxMajorVersions;
        Feed.RetentionMaxMinorVersions = UseGlobalRetentionMinor ? null : RetentionMaxMinorVersions;
        Feed.RetentionMaxPatchVersions = UseGlobalRetentionPatch ? null : RetentionMaxPatchVersions;
        Feed.RetentionMaxPrereleaseVersions = UseGlobalRetentionPrerelease ? null : RetentionMaxPrereleaseVersions;

        Feed.MirrorEnabled = MirrorEnabled;
        Feed.MirrorPackageSource = string.IsNullOrWhiteSpace(MirrorPackageSource) ? null : MirrorPackageSource.Trim();
        Feed.MirrorLegacy = MirrorLegacy;
        Feed.MirrorDownloadTimeoutSeconds = MirrorDownloadTimeoutSeconds;
        Feed.MirrorAuthType = MirrorAuthType;
        Feed.MirrorAuthUsername = string.IsNullOrWhiteSpace(MirrorAuthUsername) ? null : MirrorAuthUsername.Trim();
        if (!string.IsNullOrWhiteSpace(MirrorAuthCustomHeaders))
        {
            var headersJson = MirrorAuthCustomHeaders.Trim();
            if (!TryValidateCustomHeaders(headersJson, out var headersError))
            {
                ErrorMessage = headersError;
                PopulateFromFeed(Feed);
                return Page();
            }
            Feed.MirrorAuthCustomHeaders = headersJson;
        }
        else
        {
            Feed.MirrorAuthCustomHeaders = null;
        }

        // Only overwrite secrets if a new value was provided — leave blank to keep existing
        if (!string.IsNullOrEmpty(MirrorAuthPasswordNew))
            Feed.MirrorAuthPassword = MirrorAuthPasswordNew;

        if (!string.IsNullOrEmpty(MirrorAuthTokenNew))
            Feed.MirrorAuthToken = MirrorAuthTokenNew;

        await _feedService.UpdateFeedAsync(Feed, cancellationToken);

        SuccessMessage = "Settings saved.";
        PopulateFromFeed(Feed);
        return Page();
    }
}
