# BaGetter's SQLite Database Provider

This project contains BaGetter's SQLite database provider.

## Migrations

Check for pending changes with:
```
dotnet ef migrations has-pending-model-changes --context SqliteContext --startup-project ..\BaGetter\BaGetter.csproj
```

Add a migration with:

```
dotnet ef migrations add MigrationName --context SqliteContext --output-dir Migrations --startup-project ..\BaGetter\BaGetter.csproj
```

Apply the migration to your database with:

```
dotnet ef database update --context SqliteContext
```
