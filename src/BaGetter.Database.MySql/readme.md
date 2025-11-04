# BaGetter's MySQL Database Provider

This project contains BaGetter's MySQL database provider.

## Migrations

Check for pending changes with:
```
dotnet ef migrations has-pending-model-changes --context MySqlContext --startup-project ..\BaGetter\BaGetter.csproj
```

Add a migration with:

```
dotnet ef migrations add MigrationName --context MySqlContext --output-dir Migrations --startup-project ..\BaGetter\BaGetter.csproj
```

Apply the migration to your database with:

```
dotnet ef database update --context MySqlContext
```
