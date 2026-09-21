using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BaGetter.Core;
using NuGet.Packaging;
using NuGet.Versioning;

namespace BaGetter.Web.Admin;

public sealed class AdminPackageService : IAdminPackageService
{
    private readonly IPackageDatabase _packages;
    private readonly IPackageStorageService _storage;
    private readonly IPackageIndexingService _indexer;
    private readonly ISearchIndexer _searchIndexer;

    public AdminPackageService(
        IPackageDatabase packages,
        IPackageStorageService storage,
        IPackageIndexingService indexer,
        ISearchIndexer searchIndexer)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
        _searchIndexer = searchIndexer ?? throw new ArgumentNullException(nameof(searchIndexer));
    }

    public async Task<AdminPackageOperationResult> SetListedAsync(
        string id,
        NuGetVersion version,
        bool listed,
        CancellationToken cancellationToken)
    {
        var package = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
        if (package == null)
        {
            return AdminPackageOperationResult.Failure("Package version was not found.");
        }

        var changed = listed
            ? await _packages.RelistPackageAsync(id, version, cancellationToken)
            : await _packages.UnlistPackageAsync(id, version, cancellationToken);

        if (!changed)
        {
            return AdminPackageOperationResult.Failure("Package version was not found.");
        }

        await _searchIndexer.IndexAsync(package, cancellationToken);
        return AdminPackageOperationResult.Success(
            listed ? "Package version was relisted." : "Package version was unlisted.");
    }

    public async Task<AdminPackageOperationResult> HardDeleteAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        var package = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
        if (package == null)
        {
            return AdminPackageOperationResult.Failure("Package version was not found.");
        }

        if (!await _packages.HardDeletePackageAsync(id, version, cancellationToken))
        {
            return AdminPackageOperationResult.Failure("Package version was not found.");
        }

        await _storage.DeleteAsync(id, version, cancellationToken);
        await _searchIndexer.IndexAsync(package, cancellationToken);
        return AdminPackageOperationResult.Success("Package version was permanently deleted.");
    }

    public Task<AdminPackageOperationResult> CopyAsync(
        string id,
        NuGetVersion version,
        string targetId,
        CancellationToken cancellationToken)
    {
        return CopyCoreAsync(id, version, targetId, cancellationToken);
    }

    public async Task<AdminPackageOperationResult> RenameAsync(
        string id,
        NuGetVersion version,
        string targetId,
        CancellationToken cancellationToken)
    {
        var copyResult = await CopyCoreAsync(id, version, targetId, cancellationToken);
        if (!copyResult.Succeeded)
        {
            return copyResult;
        }

        var deleteResult = await HardDeleteAsync(id, version, cancellationToken);
        if (!deleteResult.Succeeded)
        {
            return AdminPackageOperationResult.Failure(
                $"Copied package to '{targetId}', but could not delete the source: {deleteResult.Message}");
        }

        return AdminPackageOperationResult.Success($"Package version was renamed to '{targetId}'.");
    }

    private async Task<AdminPackageOperationResult> CopyCoreAsync(
        string id,
        NuGetVersion version,
        string targetId,
        CancellationToken cancellationToken)
    {
        targetId = targetId?.Trim();
        if (string.IsNullOrWhiteSpace(targetId) || !PackageIdValidator.IsValidPackageId(targetId))
        {
            return AdminPackageOperationResult.Failure("The target package ID is invalid.");
        }

        if (string.Equals(id, targetId, StringComparison.OrdinalIgnoreCase))
        {
            return AdminPackageOperationResult.Failure("The target package ID must be different.");
        }

        if (await _packages.ExistsAsync(targetId, version, cancellationToken))
        {
            return AdminPackageOperationResult.Failure("The target package version already exists.");
        }

        var sourcePackage = await _packages.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
        if (sourcePackage == null)
        {
            return AdminPackageOperationResult.Failure("Package version was not found.");
        }

        await using var source = await _storage.GetPackageStreamAsync(id, version, cancellationToken);
        await using var transformed = new MemoryStream();
        await source.CopyToAsync(transformed, cancellationToken);
        transformed.Position = 0;

        using (var archive = new ZipArchive(transformed, ZipArchiveMode.Update, leaveOpen: true))
        {
            if (archive.Entries.Any(entry =>
                    string.Equals(entry.FullName, ".signature.p7s", StringComparison.OrdinalIgnoreCase)))
            {
                return AdminPackageOperationResult.Failure(
                    "Signed packages cannot be copied or renamed because changing the ID would invalidate the signature.");
            }

            var nuspecEntry = archive.Entries.SingleOrDefault(entry =>
                entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            if (nuspecEntry == null)
            {
                return AdminPackageOperationResult.Failure("The package does not contain a nuspec manifest.");
            }

            XDocument document;
            await using (var nuspecStream = nuspecEntry.Open())
            {
                document = await XDocument.LoadAsync(nuspecStream, LoadOptions.PreserveWhitespace, cancellationToken);
            }

            var idElement = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "id");
            if (idElement == null)
            {
                return AdminPackageOperationResult.Failure("The package manifest does not contain an ID.");
            }

            idElement.Value = targetId;
            nuspecEntry.Delete();

            var newNuspec = archive.CreateEntry($"{targetId}.nuspec", CompressionLevel.Optimal);
            await using var newNuspecStream = newNuspec.Open();
            await document.SaveAsync(newNuspecStream, SaveOptions.DisableFormatting, cancellationToken);
        }

        transformed.Position = 0;
        var indexingResult = await _indexer.IndexAsync(transformed, cancellationToken);
        if (indexingResult == PackageIndexingResult.Success && !sourcePackage.Listed)
        {
            var copiedPackage = await _packages.FindOrNullAsync(
                targetId, version, includeUnlisted: true, cancellationToken);
            await _packages.UnlistPackageAsync(targetId, version, cancellationToken);
            if (copiedPackage != null)
            {
                await _searchIndexer.IndexAsync(copiedPackage, cancellationToken);
            }
        }

        return indexingResult switch
        {
            PackageIndexingResult.Success =>
                AdminPackageOperationResult.Success($"Package version was copied to '{targetId}'."),
            PackageIndexingResult.PackageAlreadyExists =>
                AdminPackageOperationResult.Failure("The target package version already exists."),
            PackageIndexingResult.InvalidPackage =>
                AdminPackageOperationResult.Failure("The copied package failed validation."),
            _ => throw new InvalidOperationException($"Unknown indexing result '{indexingResult}'."),
        };
    }
}
