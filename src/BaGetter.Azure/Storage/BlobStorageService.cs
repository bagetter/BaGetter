using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using BaGetter.Core;

namespace BaGetter.Azure
{
    // See: https://github.com/NuGet/NuGetGallery/blob/master/src/NuGetGallery.Core/Services/CloudBlobCoreFileStorageService.cs
    public class BlobStorageService : IStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(BlobContainerClient container)
        {
            ArgumentNullException.ThrowIfNull(container);

            _container = container;
        }

        public async Task<Stream> GetAsync(string path, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            return await _container
                .GetBlobClient(path)
                .OpenReadAsync(new(true), cancellationToken);
        }

        public Task<Uri> GetDownloadUriAsync(string path, CancellationToken cancellationToken)
        {
            // TODO: Make expiry time configurable.
            var blob = _container.GetBlobClient(path);
            return Task.FromResult(blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddMinutes(10)));
        }

        public async Task<StoragePutResult> PutAsync(
            string path,
            Stream content,
            string contentType,
            CancellationToken cancellationToken)
        {
            var blob = _container.GetBlobClient(path);
            var condition = new BlobRequestConditions()
            {
                IfNoneMatch = new ETag("*")
            };

            try
            {
                await blob.UploadAsync(
                    content,
                    new BlobUploadOptions
                    {
                        Conditions = condition,
                    },
                    cancellationToken);

                await blob.SetHttpHeadersAsync(new BlobHttpHeaders
                {
                    ContentType = contentType,
                }, cancellationToken: cancellationToken);

                return StoragePutResult.Success;
            }
            catch (RequestFailedException e) when (e.IsAlreadyExistsException())
            {
                await using var targetStream = await blob.OpenReadAsync(new(true), cancellationToken);
                content.Position = 0;
                return content.Matches(targetStream)
                    ? StoragePutResult.AlreadyExists
                    : StoragePutResult.Conflict;
            }
        }

        public async Task DeleteAsync(string path, CancellationToken cancellationToken)
        {
            await _container
                .GetBlobClient(path)
                .DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }
}
