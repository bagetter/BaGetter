using System.ComponentModel.DataAnnotations;

namespace BaGetter.Core;

public class RequestRateLimitOptions
{
    public bool Enabled { get; set; } = false;

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 120;

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; } = 60;

    [Range(0, int.MaxValue)]
    public int QueueLimit { get; set; } = 0;
}
