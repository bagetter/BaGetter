using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BaGetter.Web.Pages.Admin;

[Authorize(AuthenticationSchemes = Core.Authentication.AuthenticationConstants.CookieScheme)]
public class FeedsModel : PageModel
{
    private static readonly Regex _slugRegex = new(@"^[a-z0-9](?:[a-z0-9-]{0,126}[a-z0-9])?$", RegexOptions.Compiled);

    private readonly IFeedService _feedService;
    private readonly IUserService _userService;
    private readonly ILogger<FeedsModel> _logger;

    public FeedsModel(IFeedService feedService, IUserService userService, ILogger<FeedsModel> logger)
    {
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<Feed> Feeds { get; set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "Slug is required.")]
    [MaxLength(128)]
    public string NewSlug { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(256)]
    public string NewName { get; set; }

    [BindProperty]
    [MaxLength(4000)]
    public string NewDescription { get; set; }

    // For edit form — slug is read-only after create, so use it as the key
    [BindProperty]
    public string EditSlug { get; set; }

    [BindProperty]
    [MaxLength(256)]
    public string EditName { get; set; }

    [BindProperty]
    [MaxLength(4000)]
    public string EditDescription { get; set; }

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

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        if (string.IsNullOrWhiteSpace(NewSlug) || string.IsNullOrWhiteSpace(NewName))
        {
            ErrorMessage = "Slug and Name are required.";
            Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
            return Page();
        }

        var slug = NewSlug.Trim().ToLowerInvariant();
        if (!_slugRegex.IsMatch(slug))
        {
            ErrorMessage = "Slug must be lowercase alphanumeric with optional hyphens (not at start/end), max 128 characters.";
            Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
            return Page();
        }

        var existing = await _feedService.GetFeedBySlugAsync(slug, cancellationToken);
        if (existing != null)
        {
            ErrorMessage = $"A feed with slug '{slug}' already exists.";
            Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
            return Page();
        }

        var feed = new Feed
        {
            Slug = slug,
            Name = NewName.Trim(),
            Description = string.IsNullOrWhiteSpace(NewDescription) ? null : NewDescription.Trim(),
        };

        await _feedService.CreateFeedAsync(feed, cancellationToken);

        SuccessMessage = $"Feed '{slug}' created successfully.";
        Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        if (string.IsNullOrWhiteSpace(EditSlug))
        {
            ErrorMessage = "Feed slug is required.";
            Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
            return Page();
        }

        var feed = await _feedService.GetFeedBySlugAsync(EditSlug, cancellationToken);
        if (feed == null)
        {
            ErrorMessage = "Feed not found.";
            Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
            return Page();
        }

        if (string.IsNullOrWhiteSpace(EditName) || EditName.Length > 256)
        {
            ErrorMessage = "Name is required and must be at most 256 characters.";
            Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
            return Page();
        }

        feed.Name = EditName.Trim();
        feed.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();

        await _feedService.UpdateFeedAsync(feed, cancellationToken);

        SuccessMessage = $"Feed '{feed.Slug}' updated successfully.";
        Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid feedId, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
            return RedirectToPage("/Index");

        try
        {
            var deleted = await _feedService.DeleteFeedAsync(feedId, cancellationToken);
            if (!deleted)
            {
                ErrorMessage = "Feed not found.";
            }
            else
            {
                SuccessMessage = "Feed deleted successfully.";
            }
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while deleting feed {FeedId}", feedId);
            ErrorMessage = "Could not delete the feed because of a database error. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting feed {FeedId}", feedId);
            ErrorMessage = "An unexpected error occurred while deleting the feed.";
        }

        Feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
        return Page();
    }
}
