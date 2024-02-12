using System;

namespace BaGetter.Protocol.Models
{
    /// <summary>
    /// A catalog leaf. Represents a single package event.<br/>
    /// Leafs can be discovered from a <see cref="CatalogPage"/>.
    /// </summary>
    /// <remarks>
    /// See: <see href="https://docs.microsoft.com/en-us/nuget/api/catalog-resource#catalog-leaf"/><br/>
    /// Based off: <see href="https://github.com/NuGet/NuGet.Services.Metadata/blob/64af0b59c5a79e0143f0808b39946df9f16cb2e7/src/NuGet.Protocol.Catalog/Models/ICatalogLeafItem.cs"/>
    /// </remarks>
    public interface ICatalogLeafItem
    {
        /// <summary>
        /// The commit timestamp of this catalog item.
        /// </summary>
        DateTimeOffset CommitTimestamp { get; }

        /// <summary>
        /// The package ID of the catalog item.
        /// </summary>
        string PackageId { get; }

        /// <summary>
        /// The package version of the catalog item.
        /// </summary>
        string PackageVersion { get; }
    }
}
