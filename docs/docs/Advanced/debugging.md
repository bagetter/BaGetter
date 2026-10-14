# Debugging BaGetter

## Quick diagnostics

1. Verify service index:

```text
GET /v3/index.json
```

2. Verify health endpoint:

```text
GET /health
```

3. Verify search and metadata:

```text
GET /v3/search?q=test
GET /v3/registration/{id}/index.json
```

## Request telemetry logs

BaGetter emits per-request telemetry logs including:

- HTTP method
- request path
- status code
- duration in ms
- endpoint display name
- trace id

Use these logs to find:

- high-latency endpoints
- repeated failures (`404`, `401`, `429`, `500`)
- request loops from clients

## Audit logs for package mutations

BaGetter emits audit entries for:

- package upload
- package delete
- package relist

Audit entries include actor and client IP metadata.

Use them to answer:

- who attempted a change
- what package and version were targeted
- whether the action was denied, failed, or successful

## Caching troubleshooting

For metadata and content endpoints, inspect:

- `ETag`
- `Cache-Control`

Expected:

- registration endpoints: `must-revalidate`
- versioned package content: `immutable`

If clients do not revalidate correctly, compare request `If-None-Match` and response `ETag`.

## Search troubleshooting

If search results appear stale:

1. Run a manual reindex:

```powershell
dotnet run --project src/BaGetter -- reindex search
```

2. Consider background reindex configuration:

- `Reindex.Enabled`
- `Reindex.RunOnStartup`
- `Reindex.IntervalMinutes`

## IIS deployment troubleshooting

Common issue: deploying build output instead of publish output.

Expected IIS deploy folder:

```text
src/BaGetter/bin/Release/net10.0/publish/
```

Use:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Publish-IIS.ps1
```

For a full production checklist, see the [Operations Playbook](operations-playbook.md).
