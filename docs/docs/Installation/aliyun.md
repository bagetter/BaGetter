# Use Alibaba Cloud (Aliyun) OSS

You can store packages to [Alibaba Cloud (Aliyun) Object Storage Service](https://www.alibabacloud.com/product/object-storage-service).

## Configure BaGetter

You can modify BaGetter's configurations by editing the `appsettings.json` file. For the full list of configurations, please refer to [BaGetter's configuration](../configuration.md) guide.

### Alibaba Cloud Object Storage Service (OSS)

Update the `appsettings.json` file:

```json
{
    ...

    "Storage": {
        "Type": "AliyunOss",
        "Endpoint": "oss-us-west-1.aliyuncs.com",
        "Bucket": "foo",
        "AccessKey": "",
        "AccessKeySecret": "",
        "Prefix": "lib/bagetter" // optional
    },

    ...
}
```
