using System.Linq;
using BaGetter.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BaGetter.Database.SqlServer;

public class SqlServerContext : AbstractContext<SqlServerContext>
{
    private readonly DatabaseOptions _bagetterOptions;

    /// <summary>
    /// The SQL Server error code for when a unique constraint is violated.
    /// </summary>
    private const int UniqueConstraintViolationErrorCode = 2627;

    public SqlServerContext(DbContextOptions<SqlServerContext> efOptions, IOptionsSnapshot<BaGetterOptions> bagetterOptions)
        : base(efOptions)
    {
        _bagetterOptions = bagetterOptions.Value.Database;
    }

    /// <summary>
    /// Check whether a <see cref="DbUpdateException"/> is due to a SQL unique constraint violation.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>Whether the exception was caused to SQL unique constraint violation.</returns>
    public override bool IsUniqueConstraintViolationException(DbUpdateException exception)
    {
        if (exception.GetBaseException() is SqlException sqlException)
        {
            return sqlException.Errors
                .OfType<SqlError>()
                .Any(error => error.Number == UniqueConstraintViolationErrorCode);
        }

        return false;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(_bagetterOptions.ConnectionString);
    }
}
