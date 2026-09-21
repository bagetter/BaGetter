using System.Collections.Generic;
using System.Text.Json.Serialization;
using BaGetter.Protocol.Models;

namespace BaGetter.Core;

/// <summary>
/// BaGetter's extensions to a registration page response.
/// </summary>
/// <remarks>Extends <see cref="RegistrationPageResponse"/>.</remarks>
public class BaGetterRegistrationPageResponse : RegistrationPageResponse
{
    /// <summary>
    /// The registration leafs in this page.
    /// </summary>
    /// <remarks>This was modified to use BaGetter's extended registration index page item model.</remarks>
    [JsonPropertyName("items")]
    [JsonPropertyOrder(int.MaxValue)]
    public new IReadOnlyList<BaGetRegistrationIndexPageItem> ItemsOrNull { get; set; }
}
