using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BaGetter.Core.Storage;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BaGetter.DataProtection;

/// <summary>
/// Persists the Data Protection key ring through BaGetter's <see cref="IStorageService"/>, so the
/// keys live wherever packages live — for every storage backend (FileSystem, Azure Blob, S3, GCS,
/// Aliyun OSS, Tencent COS). This keeps antiforgery and auth cookies valid across container restarts
/// and new revisions; without it the key ring lives in the container's ephemeral filesystem and is
/// regenerated on every restart, invalidating all existing cookies (form POSTs then fail with 400).
/// </summary>
/// <remarks>
/// <see cref="IStorageService"/> has no list operation, so all key elements are kept in a single
/// aggregate document. Key-ring writes are rare (a new key is created roughly every 90 days), and
/// BaGetter runs a single replica, so the read-modify-write here is not a practical contention point.
/// </remarks>
internal sealed class StorageServiceXmlRepository : IXmlRepository
{
    private const string KeyRingPath = "dataprotection/keyring.xml";
    private const string RootElementName = "keyRing";

    private readonly IServiceProvider _services;

    public StorageServiceXmlRepository(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        // IXmlRepository is a synchronous contract. ASP.NET Core has no synchronization context,
        // so blocking on the async storage call here cannot deadlock.
        var document = ReadAsync().GetAwaiter().GetResult();
        return document?.Root?.Elements().ToList() ?? (IReadOnlyCollection<XElement>)Array.Empty<XElement>();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);

        StoreAsync(element).GetAwaiter().GetResult();
    }

    private async Task<XDocument> ReadAsync()
    {
        using var scope = _services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

        return await TryLoadAsync(storage);
    }

    private static async Task<XDocument> TryLoadAsync(IStorageService storage)
    {
        try
        {
            await using var stream = await storage.GetAsync(KeyRingPath, CancellationToken.None);
            if (stream == null)
            {
                return null;
            }

            return await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
        }
        catch
        {
            // No key ring has been persisted yet: storage providers throw a "not found" error for a
            // missing path rather than returning null. Treat this as an empty ring. The first key is
            // written by StoreElement, whose Put/Delete surface any genuine storage error.
            return null;
        }
    }

    private async Task StoreAsync(XElement element)
    {
        using var scope = _services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var document = await TryLoadAsync(storage) ?? new XDocument(new XElement(RootElementName));
        document.Root.Add(element);

        byte[] bytes;
        using (var buffer = new MemoryStream())
        {
            document.Save(buffer);
            bytes = buffer.ToArray();
        }

        // PutAsync never overwrites existing content, so remove any current key ring first.
        // DeleteAsync is a no-op when the path doesn't exist.
        await storage.DeleteAsync(KeyRingPath, CancellationToken.None);

        using var content = new MemoryStream(bytes);
        var result = await storage.PutAsync(KeyRingPath, content, "application/xml", CancellationToken.None);
        if (result != StoragePutResult.Success)
        {
            throw new InvalidOperationException(
                $"Failed to persist the Data Protection key ring to '{KeyRingPath}': {result}.");
        }
    }
}
