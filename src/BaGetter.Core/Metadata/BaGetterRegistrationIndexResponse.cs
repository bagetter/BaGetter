using System.Collections.Generic;
using System.Text.Json.Serialization;
using BaGetter.Protocol.Models;

namespace BaGetter.Core
{
    /// <summary>
    /// BaGetter's extensions to a registration index response.
    /// </summary>
    /// <remarks>Extends <see cref="RegistrationIndexResponse"/>.</remarks>
    public class BaGetterRegistrationIndexResponse : RegistrationIndexResponse
    {
        /// <summary>
        /// The pages that contain all of the versions of the package, ordered by the package's version.
        /// </summary>
        /// <remarks>This was modified to use BaGetter's extended registration index page model.</remarks>
        [JsonPropertyName("items")]
        [JsonPropertyOrder(int.MaxValue)]
        public new IReadOnlyList<BaGetterRegistrationIndexPage> Pages { get; set; }

        /// <summary>
        /// The package's total downloads across all versions.
        /// </summary>
        /// <remarks>This is not part of the official NuGet protocol.</remarks>
        [JsonPropertyName("totalDownloads")]
        [JsonPropertyOrder(int.MaxValue)]
        public long TotalDownloads { get; set; }
    }
}
