import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Run BaGetter on Azure

:::warning

This page is a work in progress!

:::

Use Azure to scale BaGetter. You can store metadata on Azure [SQL Database](https://azure.microsoft.com/products/azure-sql/database/) or [Table Storage](https://azure.microsoft.com/products/storage/tables/), upload packages to [Azure Blob Storage](https://azure.microsoft.com/products/storage/blobs/), and soon provide powerful search using [Azure Search](https://azure.microsoft.com/services/search/).

## TODO

- App Service
- High availability setup

## Configure BaGetter

You can modify BaGetter's configurations by editing the `appsettings.json` file or through [environment variables](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#non-prefixed-environment-variables). For the full list of configurations, please refer to [BaGetter's configuration](../configuration.md) guide.

### Package Metadata Database

To storage the package metadata, you can use any [Azure SQL database](https://azure.microsoft.com/products/category/databases/) (as any normal SQL DB), but also [Azure Table Storage](https://azure.microsoft.com/products/storage/tables/) in a Storage Account or Azure Cosmod DB in Table API mode.

<Tabs>
<TabItem value="sql" label="SQL Database" default>

To use a SQL database, set the database type to the SQL provider of your choice (e.g. `SqlServer`) and provide a [connection string](https://learn.microsoft.com/ef/core/miscellaneous/connection-strings):

```json
{
    ...

    "Database": {
        "Type": "SqlServer",
        "ConnectionString": "..."
    },

    ...
}
```

</TabItem>

<TabItem value="table" label="Table Storage">

To use Azure Table Storage or Azure Cosmos DB in Table API mode, set the database type to `AzureTable` and provide a connection string and optionally a custom table name. If no table name is set, the default table name `Packages` will be used.
If it doesn't exist, it will be created automatically.

```json
{
    ...

    "Database": {
        "Type": "AzureTable",
        "ConnectionString": "...",
        "TableName": "my-nuget-packages"
    },

    ...
}
```

</TabItem>
</Tabs>

### Azure Blob Storage

Set the storage type to `AzureBlobStorage` and provide a container name to use and credentials:

<Tabs groupId="blob-storage">
<TabItem value="accessKey" label="Access Key" default>

To use account name and access key, add them like this:
```json
{
    ...

    "Storage": {
        "Type": "AzureBlobStorage",
        "Container": "my-container",
        "AccountName": "my-account",
        "AccessKey": "abcd1234"
    },

    ...
}
```

</TabItem>

<TabItem value="connectionString" label="Connection string">

To use a connection string, add it like this:

```json
{
    ...

    "Storage": {
        "Type": "AzureBlobStorage",
        "Container": "my-container",
        "ConnectionString": "AccountName=my-account;AccountKey=abcd1234;..."
    },

    ...
}
```

</TabItem>
</Tabs>

### Azure Search

Azure Search is currently not available due to significant API changes BaGetter has to make. Once it's available, you can set the search type to `AzureSearch` and provide an account name and API key:

```json
{
    ...

    "Search": {
        "Type": "AzureSearch",
        "AccountName": "my-account",
        "ApiKey": "ABCD1234"
    },

    ...
}
```

## Publish packages

Publish your first package with:

```shell
dotnet nuget push -s http://localhost:5000/v3/index.json package.1.0.0.nupkg
```

Publish your first [symbol package](https://docs.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg) with:

```shell
dotnet nuget push -s http://localhost:5000/v3/index.json symbol.package.1.0.0.snupkg
```

:::warning

You should secure your server by requiring an API Key to publish packages. For more information, please refer to the [Require an API Key](../configuration.md#require-an-api-key) guide.

:::

## Restore packages

You can restore packages by using the following package source:

`http://localhost:5000/v3/index.json`

Some helpful guides:

- [Visual Studio](https://docs.microsoft.com/nuget/consume-packages/install-use-packages-visual-studio#package-sources)
- [NuGet.config](https://docs.microsoft.com/nuget/reference/nuget-config-file#package-source-sections)

## Symbol server

You can load symbols by using the following symbol location:

`http://localhost:5000/api/download/symbols`

For Visual Studio, please refer to the [Configure Debugging](https://docs.microsoft.com/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=vs-2017#configure-symbol-locations-and-loading-options) guide.
