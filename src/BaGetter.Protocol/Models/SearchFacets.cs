using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BaGetter.Protocol.Models;

/// <summary>
/// The distinct filter values present in a feed, used to populate the search filter dropdowns.
/// </summary>
public class SearchFacets
{
    /// <summary>
    /// The distinct package types present in the feed.
    /// </summary>
    [JsonPropertyName("packageTypes")]
    public IReadOnlyList<string> PackageTypes { get; set; }

    /// <summary>
    /// The distinct target framework monikers present in the feed.
    /// </summary>
    [JsonPropertyName("frameworks")]
    public IReadOnlyList<string> Frameworks { get; set; }

    /// <summary>
    /// The distinct tags present in the feed.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; set; }
}
