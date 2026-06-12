namespace BaGetter.Core;

public class SearchOptions
{
    public string Type { get; set; }

    /// <summary>
    /// Whether search and autocomplete responses should include packages from
    /// the configured upstream mirror source.
    /// </summary>
    public bool IncludeUpstream { get; set; }
}
