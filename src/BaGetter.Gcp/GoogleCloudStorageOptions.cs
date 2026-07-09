using System.ComponentModel.DataAnnotations;
using BaGetter.Core.Configuration;

namespace BaGetter.Gcp;

public class GoogleCloudStorageOptions : StorageOptions
{
    [Required]
    public string BucketName { get; set; }
}
