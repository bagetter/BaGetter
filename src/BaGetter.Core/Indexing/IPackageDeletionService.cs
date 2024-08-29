using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace BaGetter.Core;

public interface IPackageDeletionService
{
    /// <summary>
    /// Delete old versions of packages
    /// </summary>
    /// <param name="package">Current package object to clean</param>
    /// <param name="maxPackagesToKeep">Maximum number of packages to keep</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Number of packages deleted</returns>
    Task<int> DeleteOldVersionsAsync(Package package, uint maxPackagesToKeep, CancellationToken cancellationToken);

    /// <summary>
    /// Attempt to delete a package.
    /// </summary>
    /// <param name="id">The id of the package to delete.</param>
    /// <param name="version">The version of the package to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>False if the package does not exist.</returns>
    Task<bool> TryDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken);
}
