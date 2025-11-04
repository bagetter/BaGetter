# BaGetter's PostgreSql Database Provider

This project contains BaGetter's PostgreSql database provider.

## Migrations

Check for pending changes with:
```
dotnet ef migrations has-pending-model-changes --context PostgreSqlContext --startup-project ..\BaGetter\BaGetter.csproj
```

Add a migration with:

```
dotnet ef migrations add MigrationName --context PostgreSqlContext --output-dir Migrations --startup-project ..\BaGetter\BaGetter.csproj
```

Apply the migration to your database with:

```
dotnet ef database update --context PostgreSqlContext
```
