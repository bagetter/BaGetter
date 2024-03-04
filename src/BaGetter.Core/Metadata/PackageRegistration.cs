using System;
using System.Collections.Generic;

namespace BaGetter.Core;

/// <summary>
/// The information on all versions of a package.
/// </summary>
public class PackageRegistration
{
    /// <summary>
    /// Create a new registration object.
    /// </summary>
    /// <param name="packageId"></param>
    /// <param name="packages">All versions of the package.</param>
    public PackageRegistration(string packageId, IReadOnlyList<Package> packages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(packages);

        PackageId = packageId;
        Packages = packages;
    }

    /// <summary>
    /// The package's ID.
    /// </summary>
    public string PackageId { get; }

    /// <summary>
    /// The information for each version of the package.
    /// </summary>
    public IReadOnlyList<Package> Packages { get; }
}
