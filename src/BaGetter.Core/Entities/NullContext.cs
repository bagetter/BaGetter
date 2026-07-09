using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BaGetter.Core.Entities;

public class NullContext : IContext
{
    public DatabaseFacade Database => throw new NotImplementedException();

    public DbSet<Feed> Feeds { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DbSet<Package> Packages { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DbSet<PackageDependency> PackageDependencies { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DbSet<TargetFramework> TargetFrameworks { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DbSet<User> Users { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DbSet<PersonalAccessToken> PersonalAccessTokens { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DbSet<Group> Groups { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DbSet<UserGroup> UserGroups { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DbSet<FeedPermission> FeedPermissions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool SupportsLimitInSubqueries => throw new NotImplementedException();

    public bool IsUniqueConstraintViolationException(DbUpdateException exception)
    {
        throw new NotImplementedException();
    }

    public Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
