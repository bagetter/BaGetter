using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;

namespace BaGetter.Protocol;

/// <summary>
/// The NuGet Service Index client, used to discover other resources.
/// </summary>
/// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/service-index"/></remarks>
public interface IServiceIndexClient
{
    /// <summary>
    /// Get the resources available on this package feed.
    /// </summary>
    /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/service-index#resources"/></remarks>
    /// <returns>The resources available on this package feed.</returns>
    Task<ServiceIndexResponse> GetAsync(CancellationToken cancellationToken = default);
}
