using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using BaGetter.Web.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace BaGetter.Web.Pages.Admin;

[Authorize(
    AuthenticationSchemes = AuthenticationConstants.NugetBasicAuthenticationScheme,
    Policy = AuthenticationConstants.AdminPolicy)]
public sealed class IndexModel : PageModel
{
    private const int PageSize = 25;
    private readonly IPackageDatabase _packages;
    private readonly IAdminPackageService _adminPackages;
    private readonly IAdminAuditLog _auditLog;
    private readonly IOptionsSnapshot<BaGetterOptions> _options;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPackageDatabase packages,
        IAdminPackageService adminPackages,
        IAdminAuditLog auditLog,
        IOptionsSnapshot<BaGetterOptions> options,
        ILogger<IndexModel> logger)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _adminPackages = adminPackages ?? throw new ArgumentNullException(nameof(adminPackages));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<Package> Packages { get; private set; } = [];
    public IReadOnlyList<AdminAuditEntry> AuditEntries { get; private set; } = [];
    public int TotalPackages { get; private set; }
    public int ListedPackages { get; private set; }
    public int UnlistedPackages { get; private set; }
    public int FilteredCount { get; private set; }
    public int CurrentPage { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
    public bool IsReadOnly => _options.Value.IsReadOnlyMode;

    public static string GetActionLabel(string action)
    {
        return action switch
        {
            "unlist" => "Unlisted",
            "relist" => "Relisted",
            "copy" => "Copied",
            "rename" => "Renamed",
            "delete" => "Permanently deleted",
            _ => action,
        };
    }

    [TempData]
    public string StatusMessage { get; set; }

    [TempData]
    public bool StatusSucceeded { get; set; }

    public async Task OnGetAsync(
        string q,
        string status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        CurrentPage = Math.Max(1, page);
        var listed = ParseListedFilter(status);

        TotalPackages = await _packages.CountAsync(null, null, cancellationToken);
        ListedPackages = await _packages.CountAsync(null, true, cancellationToken);
        UnlistedPackages = TotalPackages - ListedPackages;
        FilteredCount = await _packages.CountAsync(q, listed, cancellationToken);
        Packages = await _packages.SearchAsync(
            q,
            listed,
            (CurrentPage - 1) * PageSize,
            PageSize,
            cancellationToken);
        AuditEntries = await _auditLog.ReadLatestAsync(50, cancellationToken);
    }

    public async Task<IActionResult> OnPostManageAsync(
        string operation,
        string id,
        string version,
        string targetId,
        string confirmation,
        string q,
        string status,
        int page,
        CancellationToken cancellationToken)
    {
        var normalizedOperation = operation?.Trim().ToLowerInvariant() ?? string.Empty;
        AdminPackageOperationResult result;

        if (_options.Value.IsReadOnlyMode)
        {
            result = AdminPackageOperationResult.Failure("The server is in read-only mode.");
        }
        else if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            result = AdminPackageOperationResult.Failure("The package version is invalid.");
        }
        else if (normalizedOperation == "delete" &&
                 !string.Equals(confirmation?.Trim(), id, StringComparison.Ordinal))
        {
            result = AdminPackageOperationResult.Failure(
                "Permanent delete was cancelled because the confirmation did not match the package ID.");
        }
        else
        {
            try
            {
                result = normalizedOperation switch
                {
                    "unlist" => await _adminPackages.SetListedAsync(
                        id, nugetVersion, listed: false, cancellationToken),
                    "relist" => await _adminPackages.SetListedAsync(
                        id, nugetVersion, listed: true, cancellationToken),
                    "delete" => await _adminPackages.HardDeleteAsync(
                        id, nugetVersion, cancellationToken),
                    "copy" => await _adminPackages.CopyAsync(
                        id, nugetVersion, targetId, cancellationToken),
                    "rename" => await _adminPackages.RenameAsync(
                        id, nugetVersion, targetId, cancellationToken),
                    _ => AdminPackageOperationResult.Failure("Unknown package operation."),
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Administrative package operation {Operation} failed for {PackageId} {PackageVersion}",
                    normalizedOperation,
                    id,
                    version);
                result = AdminPackageOperationResult.Failure(
                    "The operation failed unexpectedly. Check the server log for details.");
            }
        }

        var actor = User.Identity?.Name ?? "unknown";
        await _auditLog.WriteAsync(
            new AdminAuditEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Actor = actor,
                Action = normalizedOperation,
                PackageId = id,
                PackageVersion = version,
                Details = BuildAuditDetails(targetId, result.Message),
                Result = result.Succeeded ? "Succeeded" : "Failed",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            },
            cancellationToken);

        StatusMessage = result.Message;
        StatusSucceeded = result.Succeeded;
        return RedirectToPage("/Admin/Index", new { q, status, page });
    }

    private static bool? ParseListedFilter(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "listed" => true,
            "unlisted" => false,
            _ => null,
        };
    }

    private static string BuildAuditDetails(string targetId, string message)
    {
        return string.IsNullOrWhiteSpace(targetId)
            ? message
            : $"targetId={targetId.Trim()}; {message}";
    }
}
