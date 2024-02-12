using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;

namespace BaGetter.Protocol.Catalog
{
    /// <summary>
    /// An interface which allows custom processing of catalog leaves.<br/>
    /// This interface should be implemented when the catalog leaf documents need to be downloaded and processed in chronological order.
    /// </summary>
    /// <remarks>Based off: <see href="https://github.com/NuGet/NuGet.Services.Metadata/blob/master/src/NuGet.Protocol.Catalog/ICatalogLeafProcessor.cs"/></remarks>
    public interface ICatalogLeafProcessor
    {
        /// <summary>
        /// Process a catalog leaf containing package details.
        /// </summary>
        /// <remarks>
        /// This method should return <see langword="false"/> or throw an exception if the catalog leaf cannot be processed.<br/>
        /// In this case, the <see cref="CatalogProcessor" /> will stop processing items.<br/>
        /// Note that the same package ID/version combination can be passed to this multiple times, for example due to an edit in the package metadata
        /// or due to a transient error and retry on the part of the <see cref="CatalogProcessor" />.
        /// </remarks>
        /// <param name="leaf">The leaf document.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns><see langword="True"/>, if the leaf was successfully processed, otherwise <see langword="false"/>.</returns>
        Task<bool> ProcessPackageDetailsAsync(PackageDetailsCatalogLeaf leaf, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process a catalog leaf containing a package delete.
        /// </summary>
        /// <remarks>
        /// This method should return <see langword="false"/> or throw an exception if the catalog leaf cannot be processed.<br/>
        /// In this case, the <see cref="CatalogProcessor" /> will stop processing items.<br/>
        /// Note that the same package ID/version combination can be passed to this multiple times, for example due to a package being deleted
        /// again due to a transient error and retry on the part of the <see cref="CatalogProcessor" />.
        /// </remarks>
        /// <param name="leaf">The leaf document.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns><see langword="True"/>, if the leaf was successfully processed, otherwise <see langword="false"/>.</returns>
        Task<bool> ProcessPackageDeleteAsync(PackageDeleteCatalogLeaf leaf, CancellationToken cancellationToken = default);
    }
}
