using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BaGetter.Core.Entities;

public interface IContext
{
    DatabaseFacade Database { get; }

    DbSet<Feed> Feeds { get; set; }
    DbSet<Package> Packages { get; set; }
    DbSet<PackageDependency> PackageDependencies { get; set; }
    DbSet<TargetFramework> TargetFrameworks { get; set; }
    DbSet<User> Users { get; set; }
    DbSet<PersonalAccessToken> PersonalAccessTokens { get; set; }
    DbSet<Group> Groups { get; set; }
    DbSet<UserGroup> UserGroups { get; set; }
    DbSet<FeedPermission> FeedPermissions { get; set; }

    /// <summary>
    /// Check whether a <see cref="DbUpdateException"/> is due to a SQL unique constraint violation.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>Whether the exception was caused to SQL unique constraint violation.</returns>
    bool IsUniqueConstraintViolationException(DbUpdateException exception);

    /// <summary>
    /// Whether this database engine supports LINQ "Take" in subqueries.
    /// </summary>
    bool SupportsLimitInSubqueries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Applies any pending migrations for the context to the database.
    /// Creates the database if it does not already exist.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>A task that completes once migrations are applied.</returns>
    Task RunMigrationsAsync(CancellationToken cancellationToken);
}
