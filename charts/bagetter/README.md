# bagetter Helm Chart

![Version: 1.6.0](https://img.shields.io/badge/Version-1.6.0-informational?style=flat-square) ![Type: application](https://img.shields.io/badge/Type-application-informational?style=flat-square) ![AppVersion: 1.6.0](https://img.shields.io/badge/AppVersion-1.6.0-informational?style=flat-square)

Deploy [BaGetter](https://www.bagetter.com/) — an open source NuGet server — to Kubernetes with opinionated defaults and optional integrations for major storage and database providers.

**Homepage:** <https://github.com/bagetter/BaGetter/tree/main/charts/bagetter>

## Quick Start

```bash
helm repo add bagetter https://bagetter.github.io/helm-charts
helm install my-bagetter bagetter/bagetter
```

Upgrade after editing `values.yaml`:

```bash
helm upgrade my-bagetter bagetter/bagetter -f custom-values.yaml
```

## Documentation

Detailed guides live in the [`docs/`](./docs) folder and are published to <https://bagetter.github.io/helm-chart-repo/docs/index.html>.

- [Overview](./docs/overview.md)
- [Storage configuration](./docs/storage.md)
- [Database providers](./docs/database.md)
- [Additional configuration patterns](./docs/configuration.md)
- [Testing & validation](./docs/testing.md)
- [Upgrade guidance](./docs/upgrading.md)

## Testing

Install the [helm-unittest](https://github.com/helm-unittest/helm-unittest) plugin and run:

```bash
helm unittest charts/bagetter
```

The repo keeps lint and unit tests up to date in CI to catch template regressions.

## Source Links

- <https://github.com/bagetter/BaGetter>
- <https://hub.docker.com/r/bagetter/bagetter>
