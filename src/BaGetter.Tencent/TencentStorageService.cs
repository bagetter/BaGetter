using BaGetter.Core;

namespace BaGetter.Tencent;

public class TencentStorageService : IStorageService
{
    private readonly TencentCosClient _cosClient;
    private readonly TencentStorageOptions _options;

    public TencentStorageService(TencentCosClient cosClient, TencentStorageOptions options)
    {
        _cosClient = cosClient;
        _options = options;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var found = _cosClient.BucketClient.GetDirFiles(path).FirstOrDefault() ?? string.Empty;
        if(string.IsNullOrEmpty(found))
        {
            throw new KeyNotFoundException($"The file {path} was not found.");
        }
        _cosClient.BucketClient.DeleteDir(path);
    }

    public async Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var fileStorageUrl = _cosClient.BucketClient.GetDirFiles(path).FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrEmpty(fileStorageUrl))
        {
            throw new KeyNotFoundException($"The file {path} was not found.");
        }
        var bytes = _cosClient.BucketClient.DownloadFileBytes(fileStorageUrl);
        return new MemoryStream(bytes);
    }

    public async Task<Uri> GetDownloadUriAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var fileStorageUrl = _cosClient.BucketClient.GetDirFiles(path).FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrEmpty(fileStorageUrl))
        {
            throw new KeyNotFoundException($"The file {path} was not found.");
        }
        return new Uri(fileStorageUrl);
    }

    public async Task<StoragePutResult> PutAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        using var seekableContent = new MemoryStream();
        await content.CopyToAsync(seekableContent, 4096, cancellationToken);

        seekableContent.Seek(0, SeekOrigin.Begin);

        var fileRet = _cosClient.BucketClient.UploadStream(path, seekableContent);
        if(!fileRet)
        {
            return StoragePutResult.AlreadyExists;
        }
        return StoragePutResult.Success;
    }
}
