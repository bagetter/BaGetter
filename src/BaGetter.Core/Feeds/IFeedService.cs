using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;

namespace BaGetter.Core.Feeds;

public interface IFeedService
{
    Task<Feed> GetDefaultFeedAsync(CancellationToken cancellationToken);
    Task<Feed> GetFeedByIdAsync(Guid feedId, CancellationToken cancellationToken);
    Task<Feed> GetFeedBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<List<Feed>> GetAllFeedsAsync(CancellationToken cancellationToken);
    Task<Feed> CreateFeedAsync(Feed feed, CancellationToken cancellationToken);
    Task<Feed> UpdateFeedAsync(Feed feed, CancellationToken cancellationToken);
    Task<bool> DeleteFeedAsync(Guid feedId, CancellationToken cancellationToken);
    Task EnsureDefaultFeedExistsAsync(CancellationToken cancellationToken);
}
