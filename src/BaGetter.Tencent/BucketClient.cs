using COSXML;
using COSXML.CosException;
using COSXML.Model.Bucket;
using COSXML.Model.Object;
using COSXML.Model.Tag;

namespace BaGetter.Tencent;

public class BucketClient
{
    private readonly CosXmlServer _cosXml;
    private readonly string _fullBucketName;
    private readonly string _appId;
    private readonly string _region;

    public CosXmlServer CosXml => _cosXml;

    public BucketClient(CosXmlServer cosXml, string fullBucketName, string appId, string region)
    {
        _cosXml = cosXml;
        _fullBucketName = fullBucketName;
        _appId = appId;
        _region = region;
    }

    public bool UploadStream(string key, Stream stream)
    {
        var result = false;
        try
        {
            var request = new PutObjectRequest(_fullBucketName, key, stream);

            var resultData = CosXml.PutObject(request);

            if (resultData != null && resultData.IsSuccessful())
            {
                result = true;
            }
        }
        catch (CosClientException clientEx)
        {
            throw new Exception(clientEx.Message);
        }
        catch (CosServerException serverEx)
        {
            throw new Exception(serverEx.Message);
        }
        return result;
    }

    public byte[] DownloadFileBytes(string key)
    {
        try
        {
            var request = new GetObjectBytesRequest(_fullBucketName, key);
            var result = CosXml.GetObject(request);
            if (result != null)
            {
                return result.content;
            }
        }
        catch (CosClientException clientEx)
        {
            throw new Exception(clientEx.Message);
        }
        catch (CosServerException serverEx)
        {
            throw new Exception(serverEx.Message);
        }
        return Array.Empty<byte>();
    }

    public void DeleteDir(string dir)
    {
        try
        {
            string nextMarker = null;
            do
            {
                var listRequest = new GetBucketRequest(_fullBucketName);
                listRequest.SetPrefix($"{dir.TrimEnd('/')}/");
                listRequest.SetMarker(nextMarker);
                var listResult = CosXml.GetBucket(listRequest);
                var info = listResult.listBucket;
                List<ListBucket.Contents> objects = info.contentsList;
                nextMarker = info.nextMarker;

                var deleteRequest = new DeleteMultiObjectRequest(_fullBucketName);

                deleteRequest.SetDeleteQuiet(false);
                var deleteObjects = new List<string>();
                foreach (var content in objects)
                {
                    deleteObjects.Add(content.key);
                }
                deleteRequest.SetObjectKeys(deleteObjects);

                var deleteResult = CosXml.DeleteMultiObjects(deleteRequest);

            } while (nextMarker != null);
        }
        catch (CosClientException clientEx)
        {
            throw new Exception(clientEx.Message);
        }
        catch (CosServerException serverEx)
        {
            throw new Exception(serverEx.Message);
        }
    }

    public bool DoesObjectExist(string key)
    {
        var request = new DoesObjectExistRequest(_fullBucketName, key);
        return CosXml.DoesObjectExist(request);
    }
}

