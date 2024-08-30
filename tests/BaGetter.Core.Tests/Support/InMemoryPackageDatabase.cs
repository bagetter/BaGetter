using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace BaGetter.Core.Tests.Support;
public class InMemoryPackageDatabase : IPackageDatabase
{
    private readonly List<Package> _packages = new List<Package>();

    public Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
    {
        _packages.Add(package);
        return Task.FromResult(PackageAddResult.Success);
    }

    public Task AddDownloadAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        var exists = _packages.Any(p => p.Id == id && p.Version == version);
        return Task.FromResult(exists);
    }

    public Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken)
    {
        return Task.FromResult((IReadOnlyList<Package>)_packages.Where(p => p.Id == id).ToList().AsReadOnly());
    }

    public async Task<Package> FindOrNullAsync(string id, NuGetVersion version, bool includeUnlisted, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HardDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        var removed = _packages.RemoveAll(p => p.Id == id && p.Version == version);
        return Task.FromResult(removed > 0);
    }

    public async Task<bool> RelistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UnlistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}
