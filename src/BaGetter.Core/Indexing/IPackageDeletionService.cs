using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using NuGet.Versioning;

namespace BaGetter.Core.Indexing;

public interface IPackageDeletionService
{
    /// <summary>
    /// This method deletes old versions of a package.
    /// This leverages semver 2.0 - and assume a package is major.minor.patch-prerelease.build
    /// It can leverage the <see cref="IPackageDatabase"/> to list all versions of a package and then delete all but the last <paramref name="maxMajor"/> versions.
    /// It also takes into account the <paramref name="maxMinor"/>, <paramref name="maxPath"/> and <paramref name="maxPrerelease"/> parameters to further filter the versions to delete.
    /// </summary>
    /// <param name="feedId">The feed's id.</param>
    /// <param name="feedSlug">The feed's slug, used to prefix storage paths.</param>
    /// <param name="package">Package name</param>
    /// <param name="maxMajor">Maximum of major versions to keep (optional)</param>
    /// <param name="maxMinor">Maximum of minor versions to keep (optional)</param>
    /// <param name="maxPatch">Maximum of patch versions to keep (optional)</param>
    /// <param name="maxPrerelease">Maximum of pre-release versions (optional)</param>
    /// <param name="cancellationToken">Cancel the operation</param>
    /// <returns>Number of packages deleted</returns>
    Task<int> DeleteOldVersionsAsync(Guid feedId, string feedSlug, Package package, uint? maxMajor, uint? maxMinor, uint? maxPatch, uint? maxPrerelease, CancellationToken cancellationToken);

    /// <summary>
    /// Attempt to delete a package.
    /// </summary>
    /// <param name="feedId">The feed's id.</param>
    /// <param name="feedSlug">The feed's slug, used to prefix storage paths.</param>
    /// <param name="id">The id of the package to delete.</param>
    /// <param name="version">The version of the package to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>False if the package does not exist.</returns>
    Task<bool> TryDeletePackageAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// Unlist a package (soft delete): hide it from search/listings while keeping the stored file.
    /// Independent of the feed's <see cref="Configuration.PackageDeletionBehavior"/>.
    /// </summary>
    /// <param name="feedId">The feed's id.</param>
    /// <param name="id">The id of the package to unlist.</param>
    /// <param name="version">The version of the package to unlist.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>False if the package does not exist.</returns>
    Task<bool> TryUnlistPackageAsync(Guid feedId, string id, NuGetVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// Relist a previously unlisted package: make it visible in search/listings again.
    /// The inverse of <see cref="TryUnlistPackageAsync"/>.
    /// </summary>
    /// <param name="feedId">The feed's id.</param>
    /// <param name="id">The id of the package to relist.</param>
    /// <param name="version">The version of the package to relist.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>False if the package does not exist.</returns>
    Task<bool> TryRelistPackageAsync(Guid feedId, string id, NuGetVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// Hard delete a package: remove it from the database and delete its storage blobs.
    /// Independent of the feed's <see cref="Configuration.PackageDeletionBehavior"/>.
    /// </summary>
    /// <param name="feedId">The feed's id.</param>
    /// <param name="feedSlug">The feed's slug, used to prefix storage paths.</param>
    /// <param name="id">The id of the package to delete.</param>
    /// <param name="version">The version of the package to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>False if the package did not exist in the database.</returns>
    Task<bool> TryHardDeletePackageAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken);
}
