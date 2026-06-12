namespace BaGetter.Core;

public class PackageFilterRuleOptions
{
    public string PackageId { get; set; }

    public string Versions { get; set; } = "*";
}
