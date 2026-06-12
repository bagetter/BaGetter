using System.Collections.Generic;

namespace BaGetter.Core;

public class PackageFilteringOptions
{
    public bool Enabled { get; set; }

    public bool BlockCachedPackages { get; set; } = true;

    public List<PackageFilterRuleOptions> Rules { get; set; } = new();
}
