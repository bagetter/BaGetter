using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;
using NuGet.Versioning;

namespace BaGetter.Core.Metadata;

/// <inheritdoc/>
public class DefaultPackageMetadataService : IPackageMetadataService
{
    private readonly IPackageService _packages;
    private readonly RegistrationBuilder _builder;

    public DefaultPackageMetadataService(IPackageService packages, RegistrationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(builder);

        _packages = packages;
        _builder = builder;
    }

    /// <inheritdoc/>
    public async Task<BaGetterRegistrationIndexResponse> GetRegistrationIndexOrNullAsync(Guid feedId, string feedSlug, string packageId, CancellationToken cancellationToken = default)
    {
        var packages = await _packages.FindPackagesAsync(feedId, packageId, cancellationToken);
        if (!packages.Any())
        {
            return null;
        }

        return _builder.BuildIndex(
            new PackageRegistration(
                packageId,
                packages));
    }

    /// <inheritdoc/>
    public async Task<RegistrationLeafResponse> GetRegistrationLeafOrNullAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        var package = await _packages.FindPackageOrNullAsync(feedId, feedSlug, id, version, cancellationToken);
        if (package == null)
        {
            return null;
        }

        return _builder.BuildLeaf(package);
    }
}
