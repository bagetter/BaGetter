using BaGetter.Core.Entities;

namespace BaGetter.Core.Feeds;

public class FeedContext : IFeedContext
{
    public Feed CurrentFeed { get; private set; }
    public bool IsDefaultRoute { get; private set; }

    public void Set(Feed feed, bool isDefaultRoute)
    {
        CurrentFeed = feed;
        IsDefaultRoute = isDefaultRoute;
    }
}
