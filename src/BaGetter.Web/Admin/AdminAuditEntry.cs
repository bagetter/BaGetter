using System;

namespace BaGetter.Web.Admin;

public sealed class AdminAuditEntry
{
    public DateTimeOffset TimestampUtc { get; init; }
    public string Actor { get; init; }
    public string Action { get; init; }
    public string PackageId { get; init; }
    public string PackageVersion { get; init; }
    public string Details { get; init; }
    public string Result { get; init; }
    public string IpAddress { get; init; }
}
