using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using BaGetter.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BaGetter.Core.Feeds;

public class FeedService : IFeedService
{
    private static readonly Regex _slugRegex = new(
        @"^[a-z0-9](?:[a-z0-9-]{0,126}[a-z0-9])?$",
        RegexOptions.Compiled);

    private readonly IContext _context;
    private readonly IPackageStorageService _packageStorage;
    private readonly ILogger<FeedService> _logger;

    public FeedService(
        IContext context,
        IPackageStorageService packageStorage,
        ILogger<FeedService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _packageStorage = packageStorage ?? throw new ArgumentNullException(nameof(packageStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Feed> GetDefaultFeedAsync(CancellationToken cancellationToken)
    {
        return await _context.Feeds
            .FirstOrDefaultAsync(f => f.Slug == Feed.DefaultSlug, cancellationToken);
    }

    public async Task<Feed> GetFeedByIdAsync(Guid feedId, CancellationToken cancellationToken)
    {
        return await _context.Feeds
            .FirstOrDefaultAsync(f => f.Id == feedId, cancellationToken);
    }

    public async Task<Feed> GetFeedBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await _context.Feeds
            .FirstOrDefaultAsync(f => f.Slug == slug, cancellationToken);
    }

    public async Task<List<Feed>> GetAllFeedsAsync(CancellationToken cancellationToken)
    {
        // Keep the default feed on top, then sort the rest alphabetically by name.
        // Both the admin list and the nav dropdown consume this method.
        return await _context.Feeds
            .OrderByDescending(f => f.Slug == Feed.DefaultSlug)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Feed> CreateFeedAsync(Feed feed, CancellationToken cancellationToken)
    {
        if (!_slugRegex.IsMatch(feed.Slug))
        {
            throw new ArgumentException(
                $"Feed slug '{feed.Slug}' is invalid. Must match ^[a-z0-9](?:[a-z0-9-]{{0,126}}[a-z0-9])?$",
                nameof(feed));
        }

        feed.Id = Guid.NewGuid();
        feed.CreatedAtUtc = DateTime.UtcNow;
        feed.UpdatedAtUtc = DateTime.UtcNow;

        _context.Feeds.Add(feed);
        await _context.SaveChangesAsync(cancellationToken);
        return feed;
    }

    public async Task<Feed> UpdateFeedAsync(Feed feed, CancellationToken cancellationToken)
    {
        feed.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return feed;
    }

    public async Task<bool> DeleteFeedAsync(Guid feedId, CancellationToken cancellationToken)
    {
        var feed = await _context.Feeds
            .FirstOrDefaultAsync(f => f.Id == feedId, cancellationToken);

        if (feed == null)
            return false;

        if (feed.Slug == Feed.DefaultSlug)
            throw new InvalidOperationException("The default feed cannot be deleted.");

        // Feed -> Package is configured with DeleteBehavior.Restrict, so the feed row cannot be
        // removed while it still has packages. Remove the packages first, then the feed, in a
        // single SaveChanges so the whole delete is one atomic transaction.
        //
        // Dependencies and TargetFrameworks are eager-loaded and removed explicitly: their DB
        // foreign keys are NoAction (not cascade), and the relationship is optional, so EF would
        // otherwise null their PackageKey and orphan the rows rather than delete them. PackageTypes
        // is a required relationship the database cascades, so it needs no explicit removal.
        var packages = await _context.Packages
            .Where(p => p.FeedId == feedId)
            .Include(p => p.Dependencies)
            .Include(p => p.TargetFrameworks)
            .AsSingleQuery()
            .ToListAsync(cancellationToken);

        foreach (var package in packages)
        {
            _context.PackageDependencies.RemoveRange(package.Dependencies);
            _context.TargetFrameworks.RemoveRange(package.TargetFrameworks);
        }

        _context.Packages.RemoveRange(packages);
        _context.Feeds.Remove(feed);
        await _context.SaveChangesAsync(cancellationToken);

        // Best-effort cleanup of each package's stored content (nupkg/nuspec/readme/icon). The DB
        // rows are already gone, so a storage failure here must not fail the overall delete.
        foreach (var package in packages)
        {
            try
            {
                await _packageStorage.DeleteAsync(feed.Slug, package.Id, package.Version, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete stored content for package {PackageId} {PackageVersion} while deleting feed {FeedSlug}",
                    package.Id, package.Version, feed.Slug);
            }
        }

        return true;
    }

    public async Task EnsureDefaultFeedExistsAsync(CancellationToken cancellationToken)
    {
        var exists = await _context.Feeds
            .AnyAsync(f => f.Slug == Feed.DefaultSlug, cancellationToken);

        if (exists)
            return;

        _context.Feeds.Add(new Feed
        {
            Id = Feed.DefaultId,
            Slug = Feed.DefaultSlug,
            Name = "Default",
            MirrorEnabled = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
