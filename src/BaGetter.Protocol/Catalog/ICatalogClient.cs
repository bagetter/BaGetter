using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;

namespace BaGetter.Protocol
{
    /// <summary>
    /// The Catalog client, used to discover package events.<br/>
    /// You can use this resource to query for all published packages.
    /// </summary>
    /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/catalog-resource"/></remarks>
    public interface ICatalogClient
    {
        /// <summary>
        /// Get the entry point for the catalog resource.
        /// </summary>
        /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/catalog-resource#catalog-index"/></remarks>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>The catalog index.</returns>
        Task<CatalogIndex> GetIndexAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a single catalog page, used to discover catalog leafs.
        /// </summary>
        /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/catalog-resource#catalog-page"/></remarks>
        /// <param name="pageUrl">The URL of the page, from the <see cref="CatalogIndex"/>.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A catalog page.</returns>
        Task<CatalogPage> GetPageAsync(
            string pageUrl,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a single catalog leaf, representing a package deletion event.
        /// </summary>
        /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/catalog-resource#catalog-leaf"/></remarks>
        /// <param name="leafUrl">The URL of the leaf, from a <see cref="CatalogPage"/>.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A catalog leaf.</returns>
        Task<PackageDeleteCatalogLeaf> GetPackageDeleteLeafAsync(
            string leafUrl,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a single catalog leaf, representing a package creation or update event.
        /// </summary>
        /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/catalog-resource#catalog-leaf"/></remarks>
        /// <param name="leafUrl">The URL of the leaf, from a <see cref="CatalogPage"/>.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A catalog leaf.</returns>
        Task<PackageDetailsCatalogLeaf> GetPackageDetailsLeafAsync(
            string leafUrl,
            CancellationToken cancellationToken = default);
    }
}
