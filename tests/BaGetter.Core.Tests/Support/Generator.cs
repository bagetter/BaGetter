using System;
using System.Collections.Generic;
using NuGet.Versioning;

namespace BaGetter.Core.Tests.Support;

internal class Generator
{
    /// <summary>
    /// Create a fake <see cref="Package"></see> with the minimum metadata needed by the <see cref="RegistrationBuilder"></see>.
    /// </summary>
    internal static Package GetPackage(string packageId, string version, int downloads = 1)
    {
        return new Package
        {
            Id = packageId,
            Authors = new string[] { "test" },
            PackageTypes = new List<PackageType> { new PackageType { Name = "test" } },
            Dependencies = new List<PackageDependency> { },
            Version = new NuGetVersion(version),
            //Use current date for each packages publish date, because later a date offset will be
            //calculated and leads to an overflow error of the offset because the default is year 0001.
            Published = DateTime.UtcNow,
            Downloads = downloads
        };
    }
}
