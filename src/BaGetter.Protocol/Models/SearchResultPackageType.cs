using System.Text.Json.Serialization;

namespace BaGetter.Protocol.Models
{
    /// <summary>
    /// A single package type from a <see cref="SearchResult"/>.
    /// </summary>
    /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/search-query-service-resource#search-result"/></remarks>
    public class SearchResultPackageType
    {
        /// <summary>
        /// The name of the package type.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
