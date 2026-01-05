using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;
using NuGet.Versioning;

namespace BaGetter.Core;

public interface IPackageDeprecationService
{
    Task<bool> DeprecateAsync(string id, NuGetVersion version, PackageDeprecationInfo deprecation, CancellationToken cancellationToken);

    Task<PackageDeprecation> GetOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken);

    Task DeleteAsync(string id, NuGetVersion version, CancellationToken cancellationToken);

    Task AttachAsync(IEnumerable<Package> packages, CancellationToken cancellationToken);
}
