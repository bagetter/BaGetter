using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

/// <summary>
/// Provides the set of hosts that are considered trusted upstream
/// infrastructure for the configured mirror. The set is derived from the
/// upstream package source and its service index, so full proxy works for any
/// provider without a hard-coded host list.
/// </summary>
public interface IUpstreamHostProvider
{
    /// <summary>
    /// Ensure the trusted host set has been resolved and cached. No-op when full
    /// proxy or mirroring is disabled.
    /// </summary>
    Task EnsureResolvedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Whether the given host is a trusted upstream host. Returns
    /// <see langword="false"/> if the set has not been resolved yet (call
    /// <see cref="EnsureResolvedAsync"/> first).
    /// </summary>
    bool IsTrustedHost(string host);
}
