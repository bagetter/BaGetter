using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace BaGetter.Web.Admin;

public interface IAdminPackageService
{
    Task<AdminPackageOperationResult> SetListedAsync(
        string id,
        NuGetVersion version,
        bool listed,
        CancellationToken cancellationToken);

    Task<AdminPackageOperationResult> HardDeleteAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken);

    Task<AdminPackageOperationResult> CopyAsync(
        string id,
        NuGetVersion version,
        string targetId,
        CancellationToken cancellationToken);

    Task<AdminPackageOperationResult> RenameAsync(
        string id,
        NuGetVersion version,
        string targetId,
        CancellationToken cancellationToken);
}
