# bagetter Helm Chart

![Version: 1.6.0](https://img.shields.io/badge/Version-1.6.0-informational?style=flat-square) ![Type: application](https://img.shields.io/badge/Type-application-informational?style=flat-square) ![AppVersion: 1.6.0](https://img.shields.io/badge/AppVersion-1.6.0-informational?style=flat-square)

Deploy [BaGetter](https://www.bagetter.com/) — an open source NuGet server — to Kubernetes with opinionated defaults and optional integrations for major storage and database providers.

**Homepage:** <https://github.com/bagetter/BaGetter/tree/main/charts/bagetter>

## Quick Start

```bash
helm repo add bagetter https://helm.bagetter.com
helm install my-bagetter bagetter/bagetter
```

Upgrade after editing `values.yaml`:

```bash
helm upgrade my-bagetter bagetter/bagetter -f custom-values.yaml
```

## Documentation

Detailed guides live on the BaGetter documentation site: <https://www.bagetter.com/docs/helm/overview>.

- [Overview](https://www.bagetter.com/docs/helm/overview)
- [Storage configuration](https://www.bagetter.com/docs/helm/storage)
- [Database providers](https://www.bagetter.com/docs/helm/database)
- [Additional configuration patterns](https://www.bagetter.com/docs/helm/configuration)
- [Testing & validation](https://www.bagetter.com/docs/helm/testing)
- [Upgrade guidance](https://www.bagetter.com/docs/helm/upgrading)

## Testing

Install the [helm-unittest](https://github.com/helm-unittest/helm-unittest) plugin and run:

```bash
helm unittest charts/bagetter
```

The repo keeps lint and unit tests up to date in CI to catch template regressions.

## Source Links

- <https://github.com/bagetter/BaGetter>
- <https://hub.docker.com/r/bagetter/bagetter>
