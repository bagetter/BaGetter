using COSXML;
using COSXML.Auth;

namespace BaGetter.Tencent;

public class TencentCosClient
{
    public BucketClient BucketClient { get; private set; }

    public TencentCosClient(TencentStorageOptions settings)
    {
        var cosXmlConfig = new CosXmlConfig.Builder()
             .IsHttps(true)
             .SetAppid(settings.AppId)
             .SetRegion(settings.Region)
             .Build();
        var cosCredentialProvider = new DefaultQCloudCredentialProvider(
             settings.SecretId, settings.SecretKey, settings.KeyDurationSecond);

        var cosXml = new CosXmlServer(cosXmlConfig, cosCredentialProvider);
        BucketClient = new BucketClient(cosXml, $"{settings.BucketName}-{settings.AppId}", settings.AppId, settings.Region);
    }
}
