using BaGetter.Core.Entities;

namespace BaGetter.Core.Feeds;

public interface IFeedContext
{
    Feed CurrentFeed { get; }
    bool IsDefaultRoute { get; }
}
