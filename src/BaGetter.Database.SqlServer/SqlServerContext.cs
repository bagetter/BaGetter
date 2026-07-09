using System.Linq;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SQL Server only allows one NULL in a standard unique index.
        // Use filtered indexes so multiple NULLs are permitted.
        builder.Entity<User>()
            .HasIndex(u => u.EntraObjectId)
            .IsUnique()
            .HasFilter("[EntraObjectId] IS NOT NULL");

        builder.Entity<Group>()
            .HasIndex(g => g.AppRoleValue)
            .IsUnique()
            .HasFilter("[AppRoleValue] IS NOT NULL");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(_bagetterOptions.ConnectionString);
    }
}
