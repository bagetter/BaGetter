using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;
using NuGet.Versioning;

namespace BaGetter.Core.Metadata;

/// <summary>
/// The Package Metadata client, used to fetch packages' metadata.
/// </summary>
/// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource"/></remarks>
public interface IPackageMetadataService
{
    /// <summary>
    /// Attempt to get a package's registration index, if it exists.
    /// </summary>
    /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource#registration-page"/></remarks>
    /// <param name="feedId">The feed's id.</param>
    /// <param name="feedSlug">The feed's slug, used to prefix storage paths.</param>
    /// <param name="packageId">The package's ID.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The package's <see cref="BaGetterRegistrationIndexResponse">registration index</see>, or <see langword="null"/> if the package does not exist.</returns>
    Task<BaGetterRegistrationIndexResponse> GetRegistrationIndexOrNullAsync(Guid feedId, string feedSlug, string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the metadata for a single package version, if the package exists.
    /// </summary>
    /// <param name="feedId">The feed's id.</param>
    /// <param name="feedSlug">The feed's slug, used to prefix storage paths.</param>
    /// <param name="packageId">The package's id.</param>
    /// <param name="packageVersion">The package's version.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The <see cref="RegistrationLeafResponse">registration leaf</see>, or <see langword="null"/> if the package does not exist.</returns>
    Task<RegistrationLeafResponse> GetRegistrationLeafOrNullAsync(Guid feedId, string feedSlug, string packageId, NuGetVersion packageVersion, CancellationToken cancellationToken = default);
}
