using System.Text.Json.Serialization;
using BaGetter.Protocol.Models;

namespace BaGetter.Core;

/// <summary>
/// BaGetter's extensions to the search result model.
/// </summary>
/// <remarks>
/// Extends <see cref="SearchResult"/>.<br/>
/// These additions are not part of the official protocol.
/// </remarks>
public class BaGetterSearchResult : SearchResult
{
    /// <summary>
    /// Whether the latest version of this package was cached from an upstream feed.
    /// </summary>
    [JsonPropertyName("cachedFromUpstream")]
    public bool CachedFromUpstream { get; set; }
}
