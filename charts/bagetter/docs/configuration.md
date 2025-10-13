# Additional Configuration Patterns

## Mirroring & Search

Enable BaGetter's read-through caching to proxy upstream feeds.

```yaml
bagetter:
  mirror:
    enabled: true
    packageSource: https://api.nuget.org/v3/index.json
    packageDownloadTimeoutSeconds: 900
    authentication:
      type: Basic
      existingSecret:
        name: bagetter-mirror
        keys:
          username: username
          password: password
  search:
    type: Database
```

Set `legacy: true` when targeting NuGet v2 feeds and choose `Bearer` or `Custom` authentication if your upstream requires tokens or custom headers.

## Package Retention & Limits

Use retention options to prune older versions automatically.

```yaml
bagetter:
  retention:
    maxMajorVersions: 5
    maxMinorVersions: 8
    maxPatchVersions: 20
    maxPrereleaseVersions: 10
```

## Authentication Options

Seed additional API keys and private-feed credentials without editing manifests.

```yaml
bagetter:
  authentication:
    apiKeys:
      - existingSecret:
          name: bagetter-extra-keys
          key: ci-api-key
      - key: inline-key
    credentials:
      - existingSecret:
          name: bagetter-feed-creds
          keys:
            username: feed-user
            password: feed-password
      - username: reader
        password: read-password
```

## Operational Controls

Adjust core BaGetter settings through `bagetter` values:

```yaml
bagetter:
  pathBase: /bagetter
  urls: "http://0.0.0.0:8080"
  runMigrationsAtStartup: false
  isReadOnlyMode: false
  packageDeletionBehavior: HardDelete
  allowPackageOverwrites: PrereleaseOnly
  maxPackageSizeGiB: 16
  statistics:
    enableStatisticsPage: true
    listConfiguredServices: true
  healthCheck:
    path: /healthz
    statusPropertyName: Status
```

Combine these with `bagetter.extraEnv` for advanced, chart-agnostic tweaks when needed.
