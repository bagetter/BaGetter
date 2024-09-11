# Use Tencent Cloud COS

You can store packages to [Tencent Cloud  Object Storage Service](https://cloud.tencent.com/document/product/436/6222).

## Configure BaGetter

You can modify BaGetter's configurations by editing the `appsettings.json` file. For the full list of configurations, please refer to [BaGetter's configuration](../configuration.md) guide.

### Tencent Cloud Object Storage Service

Update the `appsettings.json` file:

```json
{
    ...

    "Storage": {
        "Type": "TencentCos",
        "AppId": "",
        "SecretId": "",
        "SecretKey": "",
        "BucketName": "bagetter",
        "Region": "ap-guangzhou"        
    },

    ...
}
```
