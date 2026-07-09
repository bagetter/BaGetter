using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;

namespace BaGetter.Core.Feeds;

public interface IFeedSettingsResolver
{
    PackageOverwriteAllowed GetAllowPackageOverwrites(Feed feed);
    PackageDeletionBehavior GetPackageDeletionBehavior(Feed feed);
    bool GetIsReadOnlyMode(Feed feed);
    uint GetMaxPackageSizeGiB(Feed feed);
    RetentionOptions GetRetentionOptions(Feed feed);
    MirrorOptions GetMirrorOptions(Feed feed);
}
