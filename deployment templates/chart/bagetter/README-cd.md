# BaGetter CD (Jenkins + Harbor + Helm)

Brings the devop BaGetter mirror (`nuget.devop.is`) onto the same delivery path as
DevOp.Nexus. The repo `Jenkinsfile` builds the fork image, pushes it to Harbor, and
`helm upgrade`s the `bagetter` release in the `vcube` cluster.

## The job

Create a Jenkins **Pipeline** job (e.g. `NexerIQ / Infra / BaGetter Deploy`) from SCM
pointing at this repo (GitHub, `vhafdal/BaGetter`, credential: the existing GitHub
Key), script path `Jenkinsfile`. Parameters:

| Param | Default | Meaning |
|---|---|---|
| `ENVIRONMENT` | `prod` | `prod` → ns `bagetter`; `dev` → ns `bagetter-dev` (scratch, validate the chart here first) |
| `VERSION_OVERRIDE` | _(blank)_ | image tag; blank → `sha-<short sha>` (matches the current tag style) |
| `PUSH_IMAGE` | true | build + push `registry.devop.is/nexus/bagetter:<version>` |
| `DEPLOY` | true | `helm upgrade --install bagetter` |

Image lands at `registry.devop.is/nexus/bagetter` (the `nexus` Harbor project, so
`robot$jenkins-push` / `robot$k8s-pull` already have access).

## One-time prerequisites

1. **Harbor pull secret in the namespace** — the image is private:
   ```bash
   kubectl -n bagetter create secret docker-registry nexus-registry \
     --docker-server=registry.devop.is \
     --docker-username='robot$k8s-pull' --docker-password='<token>'
   ```
   (`values-prod.yaml` references it via `imagePullSecrets`.)

2. **Adopt the existing (non-Helm) release.** The live deployment was applied
   outside Helm, so the first `helm upgrade --install` will collide with existing
   objects. Either validate end-to-end in `bagetter-dev` first, or let Helm adopt
   the resources (Helm 3 adopts objects that match name/namespace/GVK once the
   `app.kubernetes.io/managed-by=Helm` + release annotations are added). Confirm the
   `bagetter-data` PVC, `bagetter-mirror`/`bagetter-secret`/`bagetter-tls` Secrets,
   and the ingress survive the cutover.

3. **DB path** — verify `Database__ConnectionString` in `values-prod.yaml` matches
   the running pod so the SQLite cache isn't orphaned (mooted by the Postgres step).

## Next: PostgreSQL (fixes the concurrency lock)

BaGetter currently runs SQLite on an NFS PVC; concurrent restore writes fail with
`database is locked`, which is why platform builds throttle to
`maxHttpRequestsPerSource=1`. With this pipeline in place we can ship the fix:

1. Provision PostgreSQL (in-cluster or managed).
2. `values-prod.yaml`: set `Database__Type: PostgreSql` and
   `Database__ConnectionString` to the Postgres DSN (via secret).
3. Deploy through this job; let the mirror re-cache (download stats reset is fine).
4. Remove the `maxHttpRequestsPerSource=1` throttle from `nexus-platform/nuget.config`.
