using BaGetter.Core.Entities;

namespace BaGetter.Core.Upstream;

public interface IUpstreamClientFactory
{
    IUpstreamClient CreateForFeed(Feed feed);
}
