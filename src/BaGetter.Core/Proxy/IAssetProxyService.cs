using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

/// <summary>
/// Fetches upstream package-resource assets server-side so clients never contact
/// the upstream directly. Enforces SSRF protection: only HTTPS URLs on trusted
/// upstream hosts that do not resolve to loopback/private addresses are fetched.
/// </summary>
public interface IAssetProxyService
{
    /// <summary>
    /// Fetch an upstream asset server-side.
    /// </summary>
    Task<AssetProxyResult> GetAssetAsync(string url, CancellationToken cancellationToken);
}

public enum AssetProxyStatus
{
    /// <summary>The asset was fetched successfully.</summary>
    Ok,

    /// <summary>The URL is not an allowed upstream asset URL (SSRF protection).</summary>
    Rejected,

    /// <summary>The URL was allowed but the asset could not be fetched.</summary>
    NotFound,
}

public sealed class AssetProxyResult
{
    public AssetProxyStatus Status { get; private init; }
    public Stream Content { get; private init; }
    public string ContentType { get; private init; }

    public static readonly AssetProxyResult Rejected = new() { Status = AssetProxyStatus.Rejected };
    public static readonly AssetProxyResult NotFound = new() { Status = AssetProxyStatus.NotFound };

    public static AssetProxyResult Ok(Stream content, string contentType) =>
        new() { Status = AssetProxyStatus.Ok, Content = content, ContentType = contentType };
}
