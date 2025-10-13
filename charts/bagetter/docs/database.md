# Database Configuration

BaGetter stores metadata in a configurable database. Set `bagetter.database.type` to `Sqlite`, `MySql`, `PostgreSql`, `SqlServer`, or `AzureTable`, and supply the relevant connection details.

## SQLite (Default)

The default deployment runs against SQLite inside the persistent volume. Override the connection string to relocate the `.db` file.

```yaml
bagetter:
  database:
    type: Sqlite
    sqlite:
      connectionString: "Data Source=/data/bagetter.db"
storage:
  persistence:
    enabled: true
```

## PostgreSQL

```bash
kubectl create secret generic bagetter-postgres \
  --from-literal=connection-string="Host=postgresql.default.svc;Port=5432;Database=bagetter;Username=bagetter;Password=s3cr3t"
```

```yaml
bagetter:
  database:
    type: PostgreSql
    postgresql:
      existingSecret:
        name: bagetter-postgres
        key: connection-string
  runMigrationsAtStartup: true
```

## MySQL / MariaDB

```yaml
bagetter:
  database:
    type: MySql
    mysql:
      connectionString: "Server=mysql.default.svc;Database=bagetter;Uid=bagetter;Pwd=changeme;Allow User Variables=true"
```

## SQL Server

```yaml
bagetter:
  database:
    type: SqlServer
    sqlServer:
      connectionString: "Server=tcp:sqlserver.default.svc,1433;Database=bagetter;User Id=bagetter;Password=Secret123;Encrypt=True;TrustServerCertificate=True"
```

## Azure Table Storage

```bash
kubectl create secret generic bagetter-azure-table \
  --from-literal=connection-string="DefaultEndpointsProtocol=https;AccountName=..."
```

```yaml
bagetter:
  database:
    type: AzureTable
    azureTable:
      tableName: Packages
      existingSecret:
        name: bagetter-azure-table
        key: connection-string
```

## Tips

- Use Kubernetes secrets to hold sensitive connection details whenever possible.
- Leave `runMigrationsAtStartup` enabled in development. In production, consider running migrations separately before scaling application replicas.
- For managed database services, ensure network connectivity (service endpoints, VPC peering, etc.) before deploying the chart.
