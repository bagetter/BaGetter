# BaGetter's SQL Server Database Provider

This project contains BaGetter's Microsoft SQL Server database provider.

## Migrations

Check for pending changes with:
```
dotnet ef migrations has-pending-model-changes --context SqlServerContext --startup-project ..\BaGetter\BaGetter.csproj
```

Add a migration with:

```
dotnet ef migrations add MigrationName --context SqlServerContext --output-dir Migrations --startup-project ..\BaGetter\BaGetter.csproj
```

Apply the migration to your database with:

```
dotnet ef database update --context SqlServerContext
```
