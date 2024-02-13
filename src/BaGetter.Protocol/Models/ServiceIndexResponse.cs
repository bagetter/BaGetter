using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BaGetter.Protocol.Models;

/// <summary>
/// The entry point for a NuGet package source used by the client to discover NuGet APIs.
/// </summary>
/// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/overview"/></remarks>
public class ServiceIndexResponse
{
    /// <summary>
    /// The service index's version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; }

    /// <summary>
    /// The resources declared by this service index.
    /// </summary>
    [JsonPropertyName("resources")]
    public IReadOnlyList<ServiceIndexItem> Resources { get; set; }
}
