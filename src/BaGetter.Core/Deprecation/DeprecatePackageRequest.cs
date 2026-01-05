using System.Collections.Generic;

namespace BaGetter.Core;

/// <summary>
/// Wire payload for deprecating a package over HTTP or CLI.
/// </summary>
public class DeprecatePackageRequest
{
    public IReadOnlyList<string> Reasons { get; set; }
    public string Message { get; set; }
    public string AlternatePackageId { get; set; }
    public string AlternatePackageVersion { get; set; }
}
