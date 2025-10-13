# Testing & Validation

## Helm Lint

Always lint custom values before deployment:

```bash
helm lint charts/bagetter -f custom-values.yaml
```

## Helm Unit Tests

Install the [helm-unittest](https://github.com/helm-unittest/helm-unittest) plugin and run the bundled smoke tests:

```bash
helm plugin install https://github.com/helm-unittest/helm-unittest   # first run
helm unittest charts/bagetter
```

The suites cover:

- Default filesystem/sqlite configuration
- Remote storage deployments (PVC skipped for S3)
- Mirror authentication with headers and secrets
- Multiple API keys and feed credentials

Run these checks in CI alongside `helm lint` to catch template regressions early.
