using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

/// <summary>
/// The result of attempting to index a package.
/// See <see cref="IPackageIndexingService.IndexAsync(Stream, string, CancellationToken)"/>.
/// </summary>
public enum PackageIndexingResult
{
    /// <summary>
    /// The package is malformed. This may also happen if BaGetter is in a corrupted state.
    /// </summary>
    InvalidPackage,

    /// <summary>
    /// The package has already been indexed.
    /// </summary>
    PackageAlreadyExists,

    /// <summary>
    /// The package has been indexed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// A dependency of the pushed package has a version range that cannot be parsed.
    /// </summary>
    InvalidDependencyVersionRange,

    /// <summary>
    /// The pushed package's manifest has no authors or no description.
    /// </summary>
    MissingRequiredMetadata,
}

/// <summary>
/// The service used to accept new packages.
/// </summary>
public interface IPackageIndexingService
{
    /// <summary>
    /// Attempt to index a new package.
    /// </summary>
    /// <param name="stream">The stream containing the package's content.</param>
    /// <param name="cacheFeedUrl">The url of the feed the package has been cached from in case of pull through caching, else null.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The result of the attempted indexing operation.</returns>
    Task<PackageIndexingResult> IndexAsync(Stream stream, string cacheFeedUrl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempt to index a new package.
    /// </summary>
    /// <param name="stream">The stream containing the package's content.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The result of the attempted indexing operation.</returns>
    Task<PackageIndexingResult> IndexAsync(Stream stream, CancellationToken cancellationToken = default);
}
