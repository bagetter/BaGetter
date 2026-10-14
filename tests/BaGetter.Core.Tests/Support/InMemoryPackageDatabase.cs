using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace BaGetter.Core.Tests.Support;
public class InMemoryPackageDatabase : IPackageDatabase
{
    private readonly List<Package> _packages = new List<Package>();

    public Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
    {
        if (_packages.Any(p => p.Id == package.Id && p.Version == package.Version))
        {
            return Task.FromResult(PackageAddResult.PackageAlreadyExists);
        }

        _packages.Add(package);
        return Task.FromResult(PackageAddResult.Success);
    }

    public Task AddDownloadAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        var package = _packages.FirstOrDefault(p => p.Id == id && p.Version == version);
        if (package != null)
        {
            package.Downloads++;
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
    {
        var exists = _packages.Any(p => p.Id == id);
        return Task.FromResult(exists);
    }

    public Task<bool> ExistsAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        var exists = _packages.Any(p => p.Id == id && p.Version == version);
        return Task.FromResult(exists);
    }

    public Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken)
    {
        var packages = _packages.Where(p => p.Id == id);
        if (!includeUnlisted)
        {
            packages = packages.Where(p => p.Listed);
        }

        return Task.FromResult((IReadOnlyList<Package>)packages.ToList().AsReadOnly());
    }

    public Task<Package> FindOrNullAsync(string id, NuGetVersion version, bool includeUnlisted, CancellationToken cancellationToken)
    {
        var package = _packages
            .Where(p => p.Id == id)
            .Where(p => p.Version == version)
            .FirstOrDefault(p => includeUnlisted || p.Listed);

        return Task.FromResult(package);
    }

    public Task<bool> HardDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        var removed = _packages.RemoveAll(p => p.Id == id && p.Version == version);
        return Task.FromResult(removed > 0);
    }

    public Task<bool> RelistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        return TryUpdatePackageAsync(id, version, p => p.Listed = true);
    }

    public Task<bool> UnlistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        return TryUpdatePackageAsync(id, version, p => p.Listed = false);
    }

    private Task<bool> TryUpdatePackageAsync(string id, NuGetVersion version, Action<Package> action)
    {
        var package = _packages.FirstOrDefault(p => p.Id == id && p.Version == version);
        if (package == null)
        {
            return Task.FromResult(false);
        }

        action(package);
        return Task.FromResult(true);
    }
}
