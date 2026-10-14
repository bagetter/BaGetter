import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Configuration

You can modify BaGetter's configurations by editing the `appsettings.json` file.

BaGetter also loads configuration from an OS-level location when present:

- Windows: `%ProgramData%\BaGetter\AppSettings.json`
- Linux: `/etc/BaGetter/AppSettings.json`

You can override the configuration root path using:

```text
BAGET_CONFIG_ROOT
```

## Startup validation rules

BaGetter validates configuration at startup and fails fast when critical settings are invalid. Current guardrails include:

- `MaxPackageSizeGiB` must be greater than `0`.
- `PathBase` must start with `/` and must not end with `/` (except `/` itself).
- `Cors.AllowAnyOrigin=true` cannot be combined with `Cors.AllowCredentials=true`.
- If `Cors.AllowAnyOrigin=false`, then `Cors.AllowedOrigins` must contain at least one value.
- `SecurityHeaders.EnableHsts=true` requires `SecurityHeaders.Enabled=true`.

These checks help catch unsafe or inconsistent runtime settings before serving traffic.

## Require an API key

You can require that users provide a password, called an API key, to publish packages.
To do so, you can insert the desired API key in the `ApiKey` field.

```json
{
    "ApiKey": "NUGET-SERVER-API-KEY",
    ...
}
```

You can also use the `ApiKeys` array in order to manage multiple API keys for multiple teams/developers.

```json
{
    "Authentication": {
        "ApiKeys": [
            {
                "Key" : "NUGET-SERVER-API-KEY-1"
            },
            {
                "Key" : "NUGET-SERVER-API-KEY-2"
            }
        ]
        ...
    }
    ...
}
```

Both `ApiKey` and `ApiKeys` work in conjunction additively eg.: `or` `||` logical operator.

You can also store hashed API keys instead of plaintext values:

- `ApiKeyHash` for the legacy single key
- `Authentication.ApiKeys[].KeyHash` for multiple keys

Supported hash format:

```text
PBKDF2$<iterations>$<base64Salt>$<base64Hash>
```

Example:

```json
{
    "ApiKeyHash": "PBKDF2$100000$<salt>$<hash>",
    "Authentication": {
        "ApiKeys": [
            {
                "KeyHash": "PBKDF2$100000$<salt>$<hash>"
            }
        ]
    }
}
```

Generate a hash from the CLI:

```shell
dotnet run --project src/BaGetter -- hash "NUGET-SERVER-API-KEY"
```

Users will now have to provide the API key to push packages:

```shell
dotnet nuget push -s http://localhost:5000/v3/index.json -k NUGET-SERVER-API-KEY package.1.0.0.nupkg
```

## Hosting on a different path

By default, BaGetter is hosted at the root path `/` (e.g. `bagetter.your-company.org`). You can host BaGetter at a different path (e.g. `bagetter.your-company.org/bagetter`) by setting the `PathBase` field:

```json
{
    ...
    "PathBase": "/bagetter",
    ...
}
```

## Enable read-through caching

Read-through caching lets you index packages from an upstream source. You can use read-through
caching to:

1. Speed up your builds if restores from [nuget.org](https://nuget.org) are slow
2. Enable package restores in offline scenarios

The following `Mirror` setting configures BaGetter to index packages from [nuget.org](https://nuget.org):

<Tabs>
  <TabItem value="None" label="No Authentication" default>
    ```json
    {
        ...

        "Mirror": {
            "Enabled":  true,
            "PackageSource": "https://api.nuget.org/v3/index.json"
        },

        ...
    }
    ```
  </TabItem>

  <TabItem value="Basic" label="Basic Authentication">
    For basic authentication, set `Type` to `Basic` and provide a `Username` and `Password`:

    ```json
    {
        ...

        "Mirror": {
            "Enabled":  true,
            "PackageSource": "https://api.nuget.org/v3/index.json",
            "Authentication": {
                "Type": "Basic",
                "Username": "username",
                "Password": "password"
            }
        },

        ...
    }
    ```
  </TabItem>

  <TabItem value="Bearer" label="Bearer Token">
    For bearer authentication, set `Type` to `Bearer` and provide a `Token`:

    ```json
    {
        ...

        "Mirror": {
            "Enabled":  true,
            "PackageSource": "https://api.nuget.org/v3/index.json",
            "Authentication": {
                "Type": "Bearer",
                "Token": "your-token"
            }
        },

        ...
    }
    ```
  </TabItem>

  <TabItem value="Custom" label="Custom Authentication">
    With the custom authentication type, you can provide any key-value pairs which will be set as headers in the request:

    ```json
    {
        ...

        "Mirror": {
            "Enabled":  true,
            "PackageSource": "https://api.nuget.org/v3/index.json",
            "Authentication": {
                "Type": "Custom",
                "CustomHeaders": {
                    "My-Auth": "your-value",
                    "Other-Header": "value"
                }
            }
        },

        ...
    }
    ```
  </TabItem>
</Tabs>

You can also configure multiple upstream mirrors using `Mirrors`.
When `Mirrors` is present and contains one or more entries, it takes precedence over `Mirror`.
BaGetter will try mirrors in order and fall back to the next mirror when a package is not found.
If `Mirrors` is absent or empty, BaGetter uses the legacy `Mirror` setting.

```json
{
    ...

    "Mirrors": [
        {
            "Enabled": true,
            "PackageSource": "https://api.nuget.org/v3/index.json"
        },
        {
            "Enabled": true,
            "PackageSource": "https://your-secondary-feed/v3/index.json"
        }
    ],

    ...
}
```


:::info

`PackageSource` is the value of the [NuGet service index](https://docs.microsoft.com/nuget/api/service-index).

:::

## Enable package hard deletions

To prevent the ["left pad" problem](https://blog.npmjs.org/post/141577284765/kik-left-pad-and-npm),
BaGetter's default configuration doesn't allow package deletions. Whenever BaGetter receives a package deletion
request, it will instead "unlist" the package. An unlisted package is undiscoverable but can still be
downloaded if you know the package's id and version. You can override this behavior by setting the
`PackageDeletionBehavior`:

```json
{
    ...

    "PackageDeletionBehavior": "HardDelete",

    ...
}
```

## Package auto-deletion

If your build server generates many nuget packages, your BaGetter server can quickly run out of space. Bagetter leverages [SemVer 2](https://semver.org/) and has logic to keep a history of packages based on the version numbering such as `<major>.<minor>.<patch>-<prerelease tag>.<prerelease build number>`.

There is an optional config section for `Retention` and the following parameters can be enabled to limit history for each level of the version. If none of these are set, there are no cleaning rules enforced. Each parameter is optional, e.g. if you specify only a `MaxPatchVersions`, the package limit will only enforced within each major and minor version combination.
Packages deleted are always the oldest based on version numbers.

- MaxMajorVersions: Maximum number of major versions for each package
- MaxMinorVersions: Maximum number of minor versions for each major version
- MaxPatchVersions: Maximum number of patch versions for each major + minor version
- MaxPrereleaseVersions: Maximum number of prerelease builds for each major + minor + patch version and prerelease type. If you have `beta` and `alpha` this will keep `MaxPrereleaseVersions` versions for both `beta` and `alpha`. Suffixes incompatible with [SemVer 2](https://semver.org/) will be treated as a separate type.

```json
{
    ...
    "Retention": {
        "MaxMajorVersions": 5,
        "MaxMinorVersions": 5,
        "MaxPatchVersions": 5,
        "MaxPrereleaseVersions": 5,
    }
    ...
}
```

## Enable package overwrites

Normally, BaGetter will reject a package upload if the id and version are already taken. This is to maintain the [immutability of semantically versioned packages](https://learn.microsoft.com/azure/devops/artifacts/artifacts-key-concepts?view=azure-devops#immutability).

:::warning

NuGet clients cache packages on multiple levels, so overwriting a package can lead to unexpected behavior.
A client may have a cached version of the package that is different from the one on the server.
Make sure that everyone involved is aware of the implications of overwriting packages.

:::

You can configure BaGetter to overwrite the already existing package by setting `AllowPackageOverwrites`:

```json
{
    ...

    "AllowPackageOverwrites": "true",

    ...
}
```

To allow pre-release versions to be overwritten but not stable releases, set `AllowPackageOverwrites` to `PrereleaseOnly`.

Pushing a package with a pre-release version like "3.1.0-SNAPSHOT" will overwrite the existing "3.1.0-SNAPSHOT" package, but pushing a "3.1.0" package will fail if a "3.1.0" package already exists.

## Private feeds

A private feed requires users to authenticate before accessing packages.

You can require that users provide a username and password to access the nuget feed.
To do so, you can insert the credentials in the `Authentication` section.

```json
{
    "Authentication": {
        "Credentials": [
            {
                "Username": "username",
                "Password": "password",
                "Roles": [ "Admin" ]
            }
        ]
        ...
    }
    ...
}
```

- `Roles` controls role-based permissions (for example `Admin` for administration-only actions in the web UI such as package deletion).

You can store a hashed password instead of plaintext:

```json
{
    "Authentication": {
        "Credentials": [
            {
                "Username": "username",
                "PasswordHash": "PBKDF2$100000$<salt>$<hash>"
            }
        ]
    }
}
```

Generate password hash:

```shell
dotnet run --project src/BaGetter -- hash "password"
```

Users will now have to provide the username and password to fetch and download packages.

### LDAP / Active Directory authentication

BaGetter can also validate package feed credentials against LDAP or Active Directory.
Configure LDAP under `Authentication.Ldap`. You can keep `Authentication.Credentials`
configured at the same time to preserve local fallback accounts for administration or
break-glass access.

```json
{
    "Authentication": {
        "Credentials": [
            {
                "Username": "local-admin",
                "PasswordHash": "PBKDF2$100000$<salt>$<hash>",
                "Roles": [ "Admin" ]
            }
        ],
        "Ldap": {
            "Enabled": true,
            "Server": "ad.example.local",
            "BaseDn": "dc=example,dc=local",
            "BindDn": "cn=bagetter,ou=Service Accounts,dc=example,dc=local",
            "BindPassword": "change-me",
            "UseSsl": true,
            "AllowedGroups": [ "NuGet Readers", "NuGet Admins" ]
        }
    }
}
```

LDAP users can authenticate with:

- `domain\username`
- `username@domain.example`

Configuration notes:

- `Authentication.Ldap.Server` and `Authentication.Ldap.BaseDn` are required when LDAP is enabled.
- `Authentication.Ldap.BindDn` and `Authentication.Ldap.BindPassword` are recommended for Active Directory because most environments do not allow anonymous directory searches.
- BaGetter adds role claims from the LDAP / Active Directory groups returned for the user. Group names are taken from each group's `CN`.
- If `Authentication.Ldap.AllowedGroups` is set, login succeeds only when the user belongs to at least one of those group names.
- If `Authentication.Ldap.AllowedGroups` is omitted or empty, no group filter is applied.
- `Authentication.Ldap.UseSsl` defaults to `true`. Use LDAPS whenever possible.
- BaGetter uses `System.DirectoryServices.Protocols`, which is cross-platform. On Linux and macOS, make sure the host trusts your LDAP server's certificate chain when using LDAPS.

How to add private nuget feed:

1. Download the latest NuGet executable.
2. Open a Command Prompt and change the path to the nuget.exe location.
3. The command from the example below stores a token in the %AppData%\NuGet\NuGet.config file. Your original credentials cannot be obtained from this token.


```shell
NuGet Sources Add -Name "localhost" -Source "http://localhost:5000/v3/index.json" -UserName "username" -Password "password"
```

If you are unable to connect to the feed by using encrypted credentials, store your credentials in clear text:

```shell
NuGet Sources Add -Name "localhost" -Source "http://localhost:5000/v3/index.json" -UserName "username" -Password "password" -StorePasswordInClearText
```

If you have already stored a token instead of storing the credentials as clear text, update the definition in the %AppData%\NuGet\NuGet.config file by using the following command:

```shell
NuGet Sources Update -Name "localhost" -Source "http://localhost:5000/v3/index.json" -UserName "username" -Password "password" -StorePasswordInClearText
```

The commands are slightly different when using the Package Manager console in Visual Studio:

```shell
dotnet nuget add source "http://localhost:5000/v3/index.json" --name "bagetter" --username "username" --password "password"
```

The same commands work for LDAP-backed feeds. Replace the username and password with your
directory credentials, for example:

```shell
dotnet nuget add source "http://localhost:5000/v3/index.json" --name "bagetter" --username "hysing\\valdi" --password "password"
```

## Database configuration

BaGetter supports multiple database engines for storing package information:

- MySQL: `MySql`
- SQLite: `Sqlite`
- SQL Server: `SqlServer`
- PostgreSQL: `PostgreSql`
- Azure Table Storage: `AzureTable`

Each database engine requires a connection string to configure the connection. Please refer to [ConnectionStrings.com](https://www.connectionstrings.com/) to learn how to create the proper connection string for each database engine.

You may configure the chosen database engine either using environment variables or by editing the `appsettings.json` file.

:::info

Database migrations are automatically applied on application startup.

:::

### Environment Variables

There are two environment variables related to database configuration. These are:

- **Database__Type**: The database engine to use, this should be one of the strings from the above list such as `PostgreSql` or `Sqlite`.
- **Database__ConnectionString**: The connection string for your database engine.

### `appsettings.json`

The database settings are located under the `Database` key in the `appsettings.json` configuration file:

```json
{
    ...

    "Database": {
        "Type": "Sqlite",
        "ConnectionString": "Data Source=bagetter.db"
    },

    ...
}
```

There are two settings related to the database configuration:

- **Type**: The database engine to use, this should be one of the strings from the above list such as `PostgreSql` or `Sqlite`.
- **ConnectionString**: The connection string for your database engine.

## IIS server options

IIS Server options can be configured under the `IISServerOptions` key. The available options are detailed at [docs.microsoft.com](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.iisserveroptions)

:::note

If not specified, the `MaxRequestBodySize` in BaGetter defaults to 250MB (262144000 bytes), rather than the ASP.NET Core default of 30MB.

:::

```json
{
    ...

    "IISServerOptions": {
        "MaxRequestBodySize": 262144000
    },

    ...
}
```

## Health Endpoint

A health endpoint is exposed at `/health` that returns 200 OK or 503 Service Unavailable and always includes a json object listing the current status of the application:

```json
{
  "Status": "Healthy",
  "Sqlite": "Healthy",
  ...
}
```

The services can be omitted by setting the `Statistics:ListConfiguredServices` to false, in which case only the `Status` property is returned in the json object.

This path and the name of the "Status" property are configurable if needed:

```json
{
    ...

    "HealthCheck": {
        "Path": "/healthz",
        "StatusPropertyName": "Status"
    },

    ...
}
```

## Maximum package size

The max package size default to 8GiB and can be configured using the `MaxPackageSizeGiB` setting. The NuGet gallery currently has a 250MB limit, which is enough for most packages.
This can be useful if you are hosting a private feed and need to host large packages that include chocolatey installers, machine learning models, etc.

```json
{
    ...

    "MaxPackageSizeGiB": 8,

    ...
}
```

## Registration paging

BaGetter serves package metadata using the NuGet registration resource. By default, versions are split into pages of 64 entries.
This helps keep registration index payloads smaller for packages with many versions.

You can configure this using `RegistrationPageSize`:

```json
{
    ...

    "RegistrationPageSize": 64,

    ...
}
```

When paging is active, the registration index (`/v3/registration/{id}/index.json`) returns page references,
and each page can be fetched from:

```text
/v3/registration/{id}/page/{lower}/{upper}.json
```

Example:

```text
/v3/registration/newtonsoft.json/page/11.0.1/13.0.3.json
```

## HTTP caching and compression

BaGetter now emits `ETag` headers for:

- registration index/page/leaf endpoints
- package versions endpoint
- package content endpoints (`.nupkg`, `.nuspec`, readme, icon)

Metadata endpoints use:

```text
Cache-Control: public, max-age=0, must-revalidate
```

Versioned package content endpoints use:

```text
Cache-Control: public, max-age=31536000, immutable
```

BaGetter also enables response compression (Brotli + Gzip) for supported responses.

## HTTP request telemetry

BaGetter logs a structured entry per request including method, path, status code, duration, endpoint, and trace identifier.

It also records in-process metrics using `System.Diagnostics.Metrics`:

- `bagetter.http.requests` (counter)
- `bagetter.http.request.duration` (histogram, milliseconds)

## Request rate limiting

You can enable fixed-window request rate limiting:

```json
{
    ...

    "RequestRateLimit": {
        "Enabled": true,
        "PermitLimit": 120,
        "WindowSeconds": 60,
        "QueueLimit": 0
    },

    ...
}
```

- `Enabled`: turn rate limiting on or off.
- `PermitLimit`: number of requests allowed in each window.
- `WindowSeconds`: window duration in seconds.
- `QueueLimit`: number of requests to queue when the limit is reached.

When enabled, rejected requests return `429 Too Many Requests`.

## CORS policy

By default, BaGetter allows all origins, methods, and headers. You can restrict this policy:

```json
{
    ...

    "Cors": {
        "AllowAnyOrigin": false,
        "AllowedOrigins": [
            "https://packages.example.com"
        ],
        "AllowAnyMethod": true,
        "AllowedMethods": [],
        "AllowAnyHeader": true,
        "AllowedHeaders": [],
        "AllowCredentials": false
    },

    ...
}
```

If `AllowAnyOrigin` is `true`, `AllowedOrigins` is ignored.

## Security headers

BaGetter can emit baseline security headers for all responses:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: SAMEORIGIN`
- `Referrer-Policy: no-referrer`
- `X-Permitted-Cross-Domain-Policies: none`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`

Configuration:

```json
{
    ...

    "SecurityHeaders": {
        "Enabled": true,
        "EnableHsts": false,
        "HstsMaxAgeDays": 365,
        "HstsIncludeSubDomains": true,
        "HstsPreload": false
    },

    ...
}
```

If `EnableHsts` is `true`, BaGetter enables HSTS outside development environments.

## Search reindexing

BaGetter supports rebuilding the search index from packages stored in the database.

Configuration:

```json
{
    ...

    "Reindex": {
        "Enabled": false,
        "RunOnStartup": false,
        "IntervalMinutes": 0,
        "BatchSize": 100
    },

    ...
}
```

- `Enabled`: enables the background reindex hosted service.
- `RunOnStartup`: run one reindex pass when the app starts.
- `IntervalMinutes`: if greater than `0`, run periodic reindex passes at this interval.
- `BatchSize`: number of packages processed per batch.

You can also trigger reindex manually from the CLI:

```shell
dotnet run --project src/BaGetter -- reindex search
```

## Audit logging

BaGetter emits audit log entries for package mutation operations:

- package upload attempts and outcomes
- package delete attempts and outcomes
- package relist attempts and outcomes

Each event includes actor and client IP metadata to simplify traceability in centralized logs.

## Statistics

On the application's statistics page the currently used services and overall package and version counts are listed.
You can hide or show this page by modifying the `EnableStatisticsPage` configuration.
If you set `ListConfiguredServices` to `false` the currently used services for database and storage (such as `Sqlite`) are omitted on the stats page:

```json
{
    ...

    "Statistics": {
        "EnableStatisticsPage": true,
        "ListConfiguredServices": false
    },

    ...
}
```

## Web UI theming

BaGetter's built-in UI can be restyled by editing:

- `src/BaGetter.Web/wwwroot/css/site.css`

The stylesheet uses CSS custom properties in `:root` for the visual system. The most important tokens are:

- `--font-body`, `--font-heading`
- `--page-bg`, `--page-text`, `--surface`, `--surface-border`
- `--brand-primary`, `--brand-accent`, `--brand-highlight`
- `--link-color`, `--link-hover`
- `--radius-sm`, `--radius-md`, `--radius-lg`

There is also a `@media (prefers-color-scheme: dark)` block for dark-mode values.

If you need different branding, you can safely override these tokens while keeping the component selectors unchanged.



## Load secrets from files

Mostly useful when running containerised (e.g. using Docker, Podman, Kubernetes, etc), the application will look for files named in the same pattern as environment variables under `/run/secrets`.

```shell
/run/secrets/Database__ConnectionString
```

This allows for sensitive values to be provided individually to the application, typically by bind-mounting files.

### Docker Compose example

```yaml
services:
  bagetter:
    image: bagetter/bagetter:latest
    volumes:
      # Single file mounted for API key
      - ./secrets/api-key.txt:/run/secrets/ApiKey:ro
      - ./data:/srv/baget
    ports:
      - "5000:8080"
    environment:
      - Database__ConnectionString=Data Source=/srv/baget/bagetter.db
      - Database__Type=Sqlite
      - Mirror__Enabled=false
      - Storage__Type=FileSystem
      - Storage__Path=/srv/baget/packages
```

The specified file `./secrets/api-key.txt` contains the clear text api key only.

The port mapping will make available the service at `http://localhost:5000`. (To make it available using `https` you should use an additional reverse proxy service, like "apache" or "nginx".)

Instead of targeting the `latest` version you may also refer to tags for major, minor and fixed releases, e.g. `1`, `1.4` or `1.4.8`.

Aditional documentation for secrets:

- [How to use secrets in Docker Compose](https://docs.docker.com/compose/use-secrets)
- [Docker Swarm secrets](https://docs.docker.com/engine/swarm/secrets)
- [Kubernetes secrets](https://kubernetes.io/docs/concepts/configuration/secret)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/#key-per-file-configuration-provider)
