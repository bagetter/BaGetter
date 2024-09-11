using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace BaGetter.Core;

public interface IPackageDeletionService
{
    /// <summary>
    /// This method deletes old versions of a package.
    /// This leverages semver 2.0 - and assume a package is major.minor.patch-prerelease.build
    /// It can leverage the <see cref="IPackageDatabase"/> to list all versions of a package and then delete all but the last <paramref name="maxMajor"/> versions.
    /// It also takes into account the <paramref name="maxMinor"/>, <paramref name="maxPath"/> and <paramref name="maxPrerelease"/> parameters to further filter the versions to delete.
    /// </summary>
    /// <param name="package">Package name</param>
    /// <param name="maxMajor">Maximum of major versions to keep (optional)</param>
    /// <param name="maxMinor">Maximum of minor versions to keep (optional)</param>
    /// <param name="maxPatch">Maximum of patch versions to keep (optional)</param>
    /// <param name="maxPrerelease">Maximum of pre-release versions (optional)</param>
    /// <param name="cancellationToken">Cancel the operation</param>
    /// <returns>Number of packages deleted</returns>
    Task<int> DeleteOldVersionsAsync(Package package, uint? maxMajor, uint? maxMinor, uint? maxPatch, uint? maxPrerelease, CancellationToken cancellationToken);

    /// <summary>
    /// Attempt to delete a package.
    /// </summary>
    /// <param name="id">The id of the package to delete.</param>
    /// <param name="version">The version of the package to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>False if the package does not exist.</returns>
    Task<bool> TryDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken);
}
