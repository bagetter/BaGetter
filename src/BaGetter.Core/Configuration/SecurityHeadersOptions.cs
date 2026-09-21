using System.ComponentModel.DataAnnotations;

namespace BaGetter.Core;

public class SecurityHeadersOptions
{
    public bool Enabled { get; set; } = true;

    public bool EnableHsts { get; set; } = false;

    [Range(1, 3650)]
    public int HstsMaxAgeDays { get; set; } = 365;

    public bool HstsIncludeSubDomains { get; set; } = true;

    public bool HstsPreload { get; set; } = false;
}
