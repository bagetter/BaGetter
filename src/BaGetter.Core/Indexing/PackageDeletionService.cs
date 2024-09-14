using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace BaGetter.Core;

public class PackageDeletionService : IPackageDeletionService
{
    private readonly IPackageDatabase _packages;
    private readonly IPackageStorageService _storage;
    private readonly BaGetterOptions _options;
    private readonly ILogger<PackageDeletionService> _logger;

    public PackageDeletionService(
        IPackageDatabase packages,
        IPackageStorageService storage,
        IOptionsSnapshot<BaGetterOptions> options,
        ILogger<PackageDeletionService> logger)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> TryDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        switch (_options.PackageDeletionBehavior)
        {
            case PackageDeletionBehavior.Unlist:
                return await TryUnlistPackageAsync(id, version, cancellationToken);

            case PackageDeletionBehavior.HardDelete:
                return await TryHardDeletePackageAsync(id, version, cancellationToken);

            default:
                throw new InvalidOperationException($"Unknown deletion behavior '{_options.PackageDeletionBehavior}'");
        }
    }

    private async Task<bool> TryUnlistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unlisting package {PackageId} {PackageVersion}...", id, version);

        if (!await _packages.UnlistPackageAsync(id, version, cancellationToken))
        {
            _logger.LogWarning("Could not find package {PackageId} {PackageVersion}", id, version);

            return false;
        }

        _logger.LogInformation("Unlisted package {PackageId} {PackageVersion}", id, version);

        return true;
    }

    private async Task<bool> TryHardDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Hard deleting package {PackageId} {PackageVersion} from the database...",
            id,
            version);

        var found = await _packages.HardDeletePackageAsync(id, version, cancellationToken);
        if (!found)
        {
            _logger.LogWarning(
                "Could not find package {PackageId} {PackageVersion} in the database",
                id,
                version);
        }

        // Delete the package from storage. This is necessary even if the package isn't
        // in the database to ensure that the storage is consistent with the database.
        _logger.LogInformation("Hard deleting package {PackageId} {PackageVersion} from storage...",
            id,
            version);

        await _storage.DeleteAsync(id, version, cancellationToken);

        _logger.LogInformation(
            "Hard deleted package {PackageId} {PackageVersion} from storage",
            id,
            version);

        return found;
    }

    public async Task<int> DeleteOldVersionsAsync(Package package, uint maxPackages, CancellationToken cancellationToken)
    {
        // list all versions of the package
        var versions = await _packages.FindAsync(package.Id, includeUnlisted: true, cancellationToken);
        if (versions is null || versions.Count <= maxPackages) return 0;
        // sort by version and take everything except the last maxPackages
        var versionsToDelete = versions
            .OrderByDescending(p => p.Version)
            .Skip((int)maxPackages)
            .ToList();
        var deleted = 0;
        foreach (var version in versionsToDelete)
        {
            if (await TryHardDeletePackageAsync(package.Id, version.Version, cancellationToken)) deleted++;
        }
        return deleted;
    }
}
