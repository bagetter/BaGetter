---
sidebar_position: 8
---

# Windows Service

BaGetter can run as a native Windows Service (without IIS).

## Prerequisites

1. Install the [.NET 10 Runtime](https://dotnet.microsoft.com/download) if you publish framework-dependent binaries.
2. Open an elevated PowerShell session (Run as Administrator).

## Publish

Publish the host project (`src/BaGetter`).

Framework-dependent:

```powershell
dotnet publish src/BaGetter/BaGetter.csproj -c Release -o C:\Services\BaGetter
```

Self-contained (no shared runtime dependency):

```powershell
dotnet publish src/BaGetter/BaGetter.csproj -c Release -r win-x64 --self-contained true -o C:\Services\BaGetter
```

## Install service

From the publish folder:

```powershell
cd C:\Services\BaGetter
.\BaGetter.exe install service --name BaGetter --start
```

Available options:

- `--name <NAME>`: Windows service name (default: `BaGetter`)
- `--display-name <DISPLAY_NAME>`: Service display name
- `--urls <URLS>`: URL bindings passed to Kestrel
- `--start`: Starts the service immediately after registration

## URL binding behavior

If `--urls` is omitted, BaGetter resolves URLs in this order:

1. `Urls` / `urls` from configuration
2. default fallback: `http://0.0.0.0:50561`

Configuration sources include local `appsettings.json` and `%ProgramData%\BaGetter\AppSettings.json`.

## Uninstall service

```powershell
.\BaGetter.exe uninstall service --name BaGetter --stop
```

Available options:

- `--name <NAME>`: Windows service name (default: `BaGetter`)
- `--stop`: Attempts to stop the service before deleting it

## Service management

Check status:

```powershell
sc.exe query BaGetter
```

Start/stop manually:

```powershell
sc.exe start BaGetter
sc.exe stop BaGetter
```

## Notes

- The install/uninstall commands are Windows-only.
- Running behind IIS is still supported; Windows Service hosting is an alternative deployment mode.
- If `Microsoft.Extensions.Hosting.WindowsServices.dll` is missing in a deployment, BaGetter falls back to normal console/IIS hosting on Windows instead of crashing at startup.
- For configuration details, see [Configuration](../configuration.md).
