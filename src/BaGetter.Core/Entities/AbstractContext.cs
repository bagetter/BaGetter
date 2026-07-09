using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaGetter.Core.Entities;

public abstract class AbstractContext<TContext> : DbContext, IContext where TContext : DbContext
{
    public const int DefaultMaxStringLength = 4000;

    public const int MaxPackageIdLength = 128;
    public const int MaxPackageVersionLength = 64;
    public const int MaxPackageMinClientVersionLength = 44;
    public const int MaxPackageLanguageLength = 20;
    public const int MaxPackageTitleLength = 256;
    public const int MaxPackageTypeNameLength = 512;
    public const int MaxPackageTypeVersionLength = 64;
    public const int MaxRepositoryTypeLength = 100;
    public const int MaxTargetFrameworkLength = 256;

    public const int MaxPackageDependencyVersionRangeLength = 256;

    public const int MaxUsernameLength = 256;
    public const int MaxDisplayNameLength = 256;
    public const int MaxEntraObjectIdLength = 128;
    public const int MaxPasswordHashLength = 256;
    public const int MaxTokenNameLength = 256;
    public const int MaxTokenHashLength = 128;
    public const int MaxTokenPrefixLength = 8;
    public const int MaxGroupNameLength = 256;
    public const int MaxAppRoleValueLength = 128;
    public const int MaxFeedIdLength = 128;

    protected AbstractContext(DbContextOptions<TContext> efOptions)
        : base(efOptions)
    { }

    public DbSet<Feed> Feeds { get; set; }
    public DbSet<Package> Packages { get; set; }
    public DbSet<PackageDependency> PackageDependencies { get; set; }
    public DbSet<PackageType> PackageTypes { get; set; }
    public DbSet<TargetFramework> TargetFrameworks { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<PersonalAccessToken> PersonalAccessTokens { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<FeedPermission> FeedPermissions { get; set; }

    public Task<int> SaveChangesAsync() => SaveChangesAsync(default);

    public virtual async Task RunMigrationsAsync(CancellationToken cancellationToken)
        => await Database.MigrateAsync(cancellationToken);

    public abstract bool IsUniqueConstraintViolationException(DbUpdateException exception);

    public virtual bool SupportsLimitInSubqueries => true;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Feed>(BuildFeedEntity);
        builder.Entity<Package>(BuildPackageEntity);
        builder.Entity<PackageDependency>(BuildPackageDependencyEntity);
        builder.Entity<PackageType>(BuildPackageTypeEntity);
        builder.Entity<TargetFramework>(BuildTargetFrameworkEntity);
        builder.Entity<User>(BuildUserEntity);
        builder.Entity<PersonalAccessToken>(BuildPersonalAccessTokenEntity);
        builder.Entity<Group>(BuildGroupEntity);
        builder.Entity<UserGroup>(BuildUserGroupEntity);
        builder.Entity<FeedPermission>(BuildFeedPermissionEntity);
    }

    private void BuildFeedEntity(EntityTypeBuilder<Feed> feed)
    {
        feed.HasKey(f => f.Id);
        feed.HasIndex(f => f.Slug).IsUnique();

        feed.Property(f => f.Slug)
            .HasMaxLength(MaxFeedIdLength)
            .IsRequired();

        feed.Property(f => f.Name)
            .HasMaxLength(MaxPackageTitleLength)
            .IsRequired();

        feed.Property(f => f.Description)
            .HasMaxLength(DefaultMaxStringLength);

        feed.Property(f => f.CreatedAtUtc).IsRequired();
        feed.Property(f => f.UpdatedAtUtc).IsRequired();

        feed.HasMany(f => f.Packages)
            .WithOne(p => p.Feed)
            .HasForeignKey(p => p.FeedId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void BuildPackageEntity(EntityTypeBuilder<Package> package)
    {
        package.HasKey(p => p.Key);
        package.HasIndex(p => new { p.FeedId, p.Id });
        package.HasIndex(p => new { p.FeedId, p.Id, p.NormalizedVersionString })
            .IsUnique();

        package.Property(p => p.Id)
            .HasMaxLength(MaxPackageIdLength)
            .IsRequired();

        package.Property(p => p.NormalizedVersionString)
            .HasColumnName("Version")
            .HasMaxLength(MaxPackageVersionLength)
            .IsRequired();

        package.Property(p => p.OriginalVersionString)
            .HasColumnName("OriginalVersion")
            .HasMaxLength(MaxPackageVersionLength);

        package.Property(p => p.ReleaseNotes)
            .HasColumnName("ReleaseNotes");

        package.Property(p => p.Authors)
            .HasMaxLength(DefaultMaxStringLength)
            .HasConversion(StringArrayToJsonConverter.Instance)
            .Metadata.SetValueComparer(StringArrayComparer.Instance);

        package.Property(p => p.IconUrl)
            .HasConversion(UriToStringConverter.Instance)
            .HasMaxLength(DefaultMaxStringLength);

        package.Property(p => p.LicenseUrl)
            .HasConversion(UriToStringConverter.Instance)
            .HasMaxLength(DefaultMaxStringLength);

        package.Property(p => p.ProjectUrl)
            .HasConversion(UriToStringConverter.Instance)
            .HasMaxLength(DefaultMaxStringLength);

        package.Property(p => p.RepositoryUrl)
            .HasConversion(UriToStringConverter.Instance)
            .HasMaxLength(DefaultMaxStringLength);

        package.Property(p => p.Tags)
            .HasMaxLength(DefaultMaxStringLength)
            .HasConversion(StringArrayToJsonConverter.Instance)
            .Metadata.SetValueComparer(StringArrayComparer.Instance);

        package.Property(p => p.Description).HasMaxLength(DefaultMaxStringLength);
        package.Property(p => p.Language).HasMaxLength(MaxPackageLanguageLength);
        package.Property(p => p.MinClientVersion).HasMaxLength(MaxPackageMinClientVersionLength);
        package.Property(p => p.Summary).HasMaxLength(DefaultMaxStringLength);
        package.Property(p => p.Title).HasMaxLength(MaxPackageTitleLength);
        package.Property(p => p.RepositoryType).HasMaxLength(MaxRepositoryTypeLength);
        package.Property(p => p.CachedFrom).HasMaxLength(DefaultMaxStringLength);

        package.Ignore(p => p.Version);
        package.Ignore(p => p.IconUrlString);
        package.Ignore(p => p.LicenseUrlString);
        package.Ignore(p => p.ProjectUrlString);
        package.Ignore(p => p.RepositoryUrlString);

        // TODO: This is needed to make the dependency to package relationship required.
        // Unfortunately, this would generate a migration that drops a foreign key, which
        // isn't supported by SQLite. The migrations will be need to be recreated for this.
        // Consumers will need to recreate their database and reindex all their packages.
        // To make this transition easier, I'd like to finish this change:
        // https://github.com/loic-sharma/BaGet/pull/174
        //package.HasMany(p => p.Dependencies)
        //    .WithOne(d => d.Package)
        //    .IsRequired();

        package.HasMany(p => p.PackageTypes)
            .WithOne(d => d.Package)
            .IsRequired();

        package.HasMany(p => p.TargetFrameworks)
            .WithOne(d => d.Package)
            .IsRequired();

        package.Property(p => p.RowVersion).IsRowVersion();
    }

    private void BuildPackageDependencyEntity(EntityTypeBuilder<PackageDependency> dependency)
    {
        dependency.HasKey(d => d.Key);
        dependency.HasIndex(d => d.Id);

        dependency.Property(d => d.Id).HasMaxLength(MaxPackageIdLength);
        dependency.Property(d => d.VersionRange).HasMaxLength(MaxPackageDependencyVersionRangeLength);
        dependency.Property(d => d.TargetFramework).HasMaxLength(MaxTargetFrameworkLength);
    }

    private void BuildPackageTypeEntity(EntityTypeBuilder<PackageType> type)
    {
        type.HasKey(d => d.Key);
        type.HasIndex(d => d.Name);

        type.Property(d => d.Name).HasMaxLength(MaxPackageTypeNameLength);
        type.Property(d => d.Version).HasMaxLength(MaxPackageTypeVersionLength);
    }

    private void BuildTargetFrameworkEntity(EntityTypeBuilder<TargetFramework> targetFramework)
    {
        targetFramework.HasKey(f => f.Key);
        targetFramework.HasIndex(f => f.Moniker);

        targetFramework.Property(f => f.Moniker).HasMaxLength(MaxTargetFrameworkLength);
    }

    private void BuildUserEntity(EntityTypeBuilder<User> user)
    {
        user.HasKey(u => u.Id);
        user.HasIndex(u => u.Username).IsUnique();
        user.HasIndex(u => u.EntraObjectId).IsUnique();

        user.Property(u => u.Username)
            .HasMaxLength(MaxUsernameLength)
            .IsRequired();

        user.Property(u => u.DisplayName)
            .HasMaxLength(MaxDisplayNameLength)
            .IsRequired();

        user.Property(u => u.AuthProvider)
            .IsRequired();

        user.Property(u => u.EntraObjectId)
            .HasMaxLength(MaxEntraObjectIdLength);

        user.Property(u => u.PasswordHash)
            .HasMaxLength(MaxPasswordHashLength);

        user.Property(u => u.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        user.Property(u => u.IsAdmin)
            .IsRequired()
            .HasDefaultValue(false);

        user.Property(u => u.CanLoginToUI)
            .IsRequired();

        user.Property(u => u.FailedLoginCount)
            .IsRequired()
            .HasDefaultValue(0);

        user.Property(u => u.CreatedAtUtc).IsRequired();
        user.Property(u => u.UpdatedAtUtc).IsRequired();

        user.HasOne(u => u.CreatedByUser)
            .WithMany()
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        user.HasMany(u => u.PersonalAccessTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        user.HasMany(u => u.UserGroups)
            .WithOne(ug => ug.User)
            .HasForeignKey(ug => ug.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void BuildPersonalAccessTokenEntity(EntityTypeBuilder<PersonalAccessToken> token)
    {
        token.HasKey(t => t.Id);
        token.HasIndex(t => t.TokenHash).IsUnique();
        token.HasIndex(t => new { t.UserId, t.Name }).IsUnique();

        token.Property(t => t.Name)
            .HasMaxLength(MaxTokenNameLength)
            .IsRequired();

        token.Property(t => t.TokenHash)
            .HasMaxLength(MaxTokenHashLength)
            .IsRequired();

        token.Property(t => t.TokenPrefix)
            .HasMaxLength(MaxTokenPrefixLength)
            .IsRequired();

        token.Property(t => t.ExpiresAtUtc).IsRequired();
        token.Property(t => t.CreatedAtUtc).IsRequired();
        token.Property(t => t.IsRevoked).IsRequired().HasDefaultValue(false);
    }

    private void BuildGroupEntity(EntityTypeBuilder<Group> group)
    {
        group.HasKey(g => g.Id);
        group.HasIndex(g => g.Name).IsUnique();

        group.Property(g => g.Name)
            .HasMaxLength(MaxGroupNameLength)
            .IsRequired();

        group.Property(g => g.AppRoleValue)
            .HasMaxLength(MaxAppRoleValueLength);

        group.HasIndex(g => g.AppRoleValue)
            .IsUnique();

        group.Property(g => g.Description)
            .HasMaxLength(DefaultMaxStringLength);

        group.Property(g => g.CreatedAtUtc).IsRequired();

        group.HasMany(g => g.UserGroups)
            .WithOne(ug => ug.Group)
            .HasForeignKey(ug => ug.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void BuildUserGroupEntity(EntityTypeBuilder<UserGroup> userGroup)
    {
        userGroup.HasKey(ug => new { ug.UserId, ug.GroupId });
    }

    private void BuildFeedPermissionEntity(EntityTypeBuilder<FeedPermission> permission)
    {
        permission.HasKey(p => p.Id);
        permission.HasIndex(p => new { p.FeedId, p.PrincipalType, p.PrincipalId }).IsUnique();

        permission.Property(p => p.FeedId).IsRequired();
        permission.Property(p => p.PrincipalType).IsRequired();
        permission.Property(p => p.PrincipalId).IsRequired();
        permission.Property(p => p.CanPush).IsRequired().HasDefaultValue(false);
        permission.Property(p => p.CanPull).IsRequired().HasDefaultValue(false);
        permission.Property(p => p.CanDelete).IsRequired().HasDefaultValue(false);
        permission.Property(p => p.Source).IsRequired().HasDefaultValue(PermissionSource.Manual);

        permission.HasOne(p => p.Feed)
            .WithMany(f => f.Permissions)
            .HasForeignKey(p => p.FeedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
