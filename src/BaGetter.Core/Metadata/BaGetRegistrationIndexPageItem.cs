using System.Text.Json.Serialization;
using BaGetter.Protocol.Models;

namespace BaGetter.Core;

/// <summary>
/// BaGetter's extensions to a registration index page.
/// </summary>
/// <remarks>Extends <see cref="RegistrationIndexPageItem"/>.</remarks>
public class BaGetRegistrationIndexPageItem : RegistrationIndexPageItem
{
    /// <summary>
    /// The catalog entry containing the package metadata.
    /// </summary>
    /// <remarks>This was modified to use BaGetter's extended package metadata model.</remarks>
    [JsonPropertyName("catalogEntry")]
    [JsonPropertyOrder(int.MaxValue)]
    public new BaGetterPackageMetadata PackageMetadata { get; set; }
}
