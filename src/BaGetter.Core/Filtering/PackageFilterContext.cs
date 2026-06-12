using NuGet.Versioning;

namespace BaGetter.Core;

public class PackageFilterContext
{
    public PackageFilterContext(string packageId, NuGetVersion version, PackageFilterScope scope)
    {
        PackageId = packageId;
        Version = version;
        Scope = scope;
    }

    public string PackageId { get; }

    public NuGetVersion Version { get; }

    public PackageFilterScope Scope { get; }
}
