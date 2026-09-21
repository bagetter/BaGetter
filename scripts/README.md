# Scripts

## `Publish-IIS.ps1`

Publishes the BaGetter host project for IIS deployments and validates that `web.config` exists in publish output.

Usage:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Publish-IIS.ps1
```

Optional parameters:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Publish-IIS.ps1 `
  -Configuration Release `
  -Project src/BaGetter/BaGetter.csproj `
  -PublishProfile FolderProfile
```

Expected publish directory:

```text
src/BaGetter/bin/Release/net10.0/publish/
```

## `SmokeTest-BaGetter.ps1`

Runs basic endpoint checks against a deployed BaGetter instance.

Usage:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/SmokeTest-BaGetter.ps1 -BaseUrl https://your-bagetter-host
```

Optional parameters:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/SmokeTest-BaGetter.ps1 `
  -BaseUrl https://your-bagetter-host `
  -PackageId TestData `
  -PackageVersion 1.2.3
```
