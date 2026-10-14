using System.ComponentModel.DataAnnotations;

namespace BaGetter.Core;

public class SearchReindexOptions
{
    public bool Enabled { get; set; } = false;

    public bool RunOnStartup { get; set; } = false;

    [Range(0, int.MaxValue)]
    public int IntervalMinutes { get; set; } = 0;

    [Range(1, int.MaxValue)]
    public int BatchSize { get; set; } = 100;
}
