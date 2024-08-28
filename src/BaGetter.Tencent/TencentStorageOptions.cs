using System.ComponentModel.DataAnnotations;

namespace BaGetter.Tencent;

public class TencentStorageOptions
{
    [Required]
    public string AppId { get; set; }
    [Required]
    public string SecretId { get; set; }
    [Required]
    public string SecretKey { get; set; }
    [Required]
    public string Region { get; set; }
    [Required]
    public string BucketName { get; set; }
    public int KeyDurationSecond { get; set; } = 600;
 
}
