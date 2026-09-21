using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Web.Admin;

public interface IAdminAuditLog
{
    Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminAuditEntry>> ReadLatestAsync(int count, CancellationToken cancellationToken);
}
