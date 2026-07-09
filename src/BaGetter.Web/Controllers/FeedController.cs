using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaGetter.Web.Controllers;

[ApiController]
[Route("api/v1/feeds")]
[Authorize(AuthenticationSchemes = AuthenticationConstants.CookieScheme)]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;
    private readonly IUserService _userService;

    public FeedController(IFeedService feedService, IUserService userService)
    {
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    [HttpGet]
    public async Task<ActionResult<List<FeedResponse>>> GetAll(CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
            return Forbid();

        var feeds = await _feedService.GetAllFeedsAsync(cancellationToken);
        return feeds.Select(FeedResponse.FromFeed).ToList();
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<FeedResponse>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
            return Forbid();

        var feed = await _feedService.GetFeedBySlugAsync(slug, cancellationToken);
        if (feed == null)
            return NotFound();

        return FeedResponse.FromFeed(feed);
    }

    [HttpPost]
    public async Task<ActionResult<FeedResponse>> Create([FromBody] CreateFeedRequest request, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var feed = new Feed
        {
            Slug = request.Slug,
            Name = request.Name,
            Description = request.Description,
            MirrorEnabled = false,
            MirrorLegacy = false,
        };

        try
        {
            var created = await _feedService.CreateFeedAsync(feed, cancellationToken);
            return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, FeedResponse.FromFeed(created));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{slug}")]
    public async Task<ActionResult<FeedResponse>> Update(string slug, [FromBody] UpdateFeedRequest request, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var feed = await _feedService.GetFeedBySlugAsync(slug, cancellationToken);
        if (feed == null)
            return NotFound();

        feed.Name = request.Name;
        feed.Description = request.Description;

        var updated = await _feedService.UpdateFeedAsync(feed, cancellationToken);
        return FeedResponse.FromFeed(updated);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(cancellationToken))
            return Forbid();

        var feed = await _feedService.GetFeedBySlugAsync(slug, cancellationToken);
        if (feed == null)
            return NotFound();

        try
        {
            var deleted = await _feedService.DeleteFeedAsync(feed.Id, cancellationToken);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private async Task<bool> IsAdminAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var userId))
            return false;

        return await _userService.IsAdminAsync(userId, cancellationToken);
    }
}
