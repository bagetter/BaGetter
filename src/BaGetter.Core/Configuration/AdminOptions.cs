namespace BaGetter.Core;

public sealed class AdminOptions
{
    /// <summary>
    /// Path to the JSON Lines audit log. Relative paths are resolved from the content root.
    /// </summary>
    public string AuditLogPath { get; set; } = "App_Data/admin-audit.jsonl";
}
