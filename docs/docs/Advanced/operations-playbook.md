---
sidebar_position: 3
---

# Operations Playbook

This page is a practical checklist for deploying, validating, and troubleshooting BaGetter in production-like environments.

## Publish and Deploy (IIS)

Always publish the host project (`src/BaGetter`), not `src/BaGetter.Web`.

```powershell
dotnet publish src/BaGetter/BaGetter.csproj -c Release -p:PublishProfile=FolderProfile
```

Expected output folder:

```text
src/BaGetter/bin/Release/net10.0/publish/
```

The publish folder must contain `web.config` for IIS hosting.

You can also run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Publish-IIS.ps1
```

## Post-Deploy Smoke Checks

Run these checks against your deployed base URL.

```powershell
$base = "https://your-bagetter-host"
Invoke-WebRequest "$base/v3/index.json" | Out-Null
Invoke-WebRequest "$base/health" | Out-Null
Invoke-WebRequest "$base/v3/search?q=test" | Out-Null
```

Or run the bundled smoke test script:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/SmokeTest-BaGetter.ps1 -BaseUrl https://your-bagetter-host
```

Check caching headers:

```powershell
$index = Invoke-WebRequest "$base/v3/registration/TestData/index.json"
$index.Headers.ETag
$index.Headers."Cache-Control"
```

Expected behavior:

- Registration endpoints include `ETag`.
- Registration endpoints use `Cache-Control` with `must-revalidate`.
- Versioned package content endpoints use `Cache-Control` with `immutable`.

## Troubleshooting: Partial Page Render / Endless Loading

If the UI partially renders and stays in a loading state:

1. Open browser network tools and look for repeating `404` on icon paths.
2. Confirm package icon endpoints return valid image content types.
3. Check server logs for repeated `AUDIT` or request telemetry patterns on the same path.

Useful server-side checks:

```powershell
Invoke-WebRequest "$base/v3/package/testdata/1.2.3/icon" -Method GET
Invoke-WebRequest "$base/v3/package/testdata/1.2.3/testdata.1.2.3.nupkg" -Method GET
```

## Reindex Search

Manual reindex:

```powershell
dotnet run --project src/BaGetter -- reindex search
```

Background reindex configuration:

```json
{
  "Reindex": {
    "Enabled": true,
    "RunOnStartup": true,
    "IntervalMinutes": 60,
    "BatchSize": 100
  }
}
```

Use this when:

- Search results are stale after migrations or provider changes.
- You changed indexing behavior and need a full rebuild.

## Audit and Request Logs

BaGetter emits:

- request telemetry logs (method, path, status, duration, endpoint, trace id)
- audit logs for upload, delete, and relist actions

Use these to answer:

- who performed a package mutation
- what action was attempted
- whether it succeeded, failed, or was denied

## Safety Checklist

- Do not store secrets in committed `appsettings.*` files.
- Keep `appsettings.Development.json` local-only.
- Restrict CORS in production (`Cors:AllowAnyOrigin=false` with explicit origins).
- Enable rate limiting for internet-facing deployments.
- Keep security headers enabled.
