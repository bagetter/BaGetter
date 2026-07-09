# AGENTS.md

This file provides guidance to AI agents when working with code in this repository.

## Guidelines for AI Agents
### 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

### 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

---

## About this project: What is BaGetter

BaGetter is a lightweight, open-source NuGet and symbol server (ASP.NET Core, .NET 10). Community fork of BaGet. Implements NuGet v3 protocol with pluggable database, storage, and search backends.

### Repo Layout (top level)

- **`src/`** — application projects (see Project Layout below).
- **`tests/`** — xUnit test projects mirroring `src/`.
- **`samples/`** — `BaGetterWebApplication` (embed BaGetter in a host app) and `BaGetter.Protocol.Samples.Tests` (protocol client examples).
- **`docs/`** — Docusaurus documentation site (Node/Yarn, `yarn.lock`); published separately from the server.
- **`deployment templates/`** — ready-to-use deployment manifests (e.g., Docker Compose, cloud).
- **`Directory.Packages.props`**, **`global.json`**, **`nuget.config`** — central package versions, pinned SDK, restore sources.
- **`Dockerfile`** — multi-stage Alpine build; publishes to `/app` and defaults `/data` for packages/symbols/db.

## Build & Test Commands

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
dotnet run --project src/BaGetter        # runs on http://localhost:5000
```

Run a single test class or method:
```bash
dotnet test --filter "FullyQualifiedName~UserServiceTests"
dotnet test --filter "FullyQualifiedName~UserServiceTests+CreateEntraUserAsync"
```

EF Core migrations (example for SQLite):
```bash
dotnet ef migrations add MigrationName --project src/BaGetter.Database.Sqlite --startup-project src/BaGetter
```

Requires .NET SDK 10.0.104 (pinned in `global.json`).

## Architecture

### Provider Pattern

Storage, database, and search services use a configuration-driven provider pattern (`IProvider<T>`). Multiple implementations coexist in DI (registered via `TryAddTransient`); the active one is selected by `appsettings.json` config at runtime. This is the core extensibility mechanism.

### Project Layout

- **`src/BaGetter/`** — ASP.NET Core host. Entry point (`Program.cs`), DI/middleware setup (`Startup.cs`), config, `ValidateBaGetterOptions`, `ConfigureBaGetterServer`, `wwwroot/` static assets.
- **`src/BaGetter.Core/`** — Business logic, EF Core entities, authentication services, configuration options, storage/search interfaces. Database-agnostic. Notable subfolders: `Indexing/`, `Metadata/`, `Search/`, `Storage/`, `Upstream/` (read-through mirror/cache), `Statistics/`, `ServiceIndex/`, `Content/`, `Validation/`, `Feeds/` (multi-feed WIP).
- **`src/BaGetter.Web/`** — HTTP controllers (`FeedController`, `PackageContentController`, `PackageMetadataController`, `PackagePublishController`, `SearchController`, `ServiceIndexController`, `SymbolController`), Razor Pages UI (`Pages/`), endpoint routing (`BaGetterEndpointBuilder.cs`), URL generation (`BaGetterUrlGenerator.cs`), `OperationCancelledMiddleware`, `libman.json` for client-side libs.
- **`src/BaGetter.Protocol/`** — NuGet v3 protocol client and models for upstream feed communication.
- **`src/BaGetter.Database.{Sqlite,SqlServer,PostgreSql,MySql}/`** — EF Core context + migrations per database provider.
- **`src/BaGetter.{Aws,Azure,Gcp,Aliyun,Tencent}/`** — Cloud storage provider implementations.

### Key Interfaces

| Interface | Purpose |
|-----------|---------|
| `IPackageDatabase` | Package CRUD operations |
| `IStorageService` | Raw blob storage |
| `IPackageStorageService` | Package-specific storage (nupkg, nuspec, readme, icon) |
| `ISearchService` / `ISearchIndexer` | Package search and indexing |
| `IContext` | EF Core DbContext abstraction |

### Authentication

Supports multiple simultaneous auth modes controlled by `AuthenticationMode` enum (`Config`, `Local`, `Entra`, `Hybrid`). `Config` is the backward-compatible mode. Auth services live in `src/BaGetter.Core/Authentication/`.

- **NuGetBasicAuth** is the default scheme; it forwards to the cookie scheme when a session cookie is present without an `Authorization` header (browser vs. client tool detection).
- **Entra ID** uses `AddMicrosoftIdentityWebApp()` (authorization code flow). `EntraRoleSyncService` syncs Entra app roles to local groups on token validation. Cookie name is `BaGetter.Auth`, 60-minute sliding session.
- **`IFeedAuthenticationService`** provides two auth paths: `AuthenticateByTokenAsync()` (PAT) and `AuthenticateByCredentialsAsync()` (username/password).
- **`FeedPermissionHandler`** (AuthorizationHandler) enforces feed-level permissions using the default feed ID `"default"`.
- Passwords use bcrypt; tokens store prefix + hash.

Authentication config structure:
```json
"Authentication": {
  "Mode": "Hybrid",
  "Entra": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": "",
    "CallbackPath": "/signin-oidc"
  },
  "MaxTokenExpiryDays": 365,
  "MaxFailedAttempts": 5,
  "LockoutMinutes": 15
}
```

### Data Model

Defined in `src/BaGetter.Core/Entities/AbstractContext.cs`. Core entities: Package, PackageDependency, PackageType, TargetFramework, User, PersonalAccessToken, Group, UserGroup, FeedPermission.

### API Endpoints

Routes defined in `BaGetterEndpointBuilder.cs`:
- Service index: `GET /v3/index.json`
- Search/autocomplete: `GET /v3/search`, `GET /v3/autocomplete`
- Package metadata: `GET /v3/registration/{id}/index.json`
- Package content: `GET /v3/package/{id}/{version}/{id}.nupkg`
- Publish: `PUT /api/v2/package`
- Symbols: `PUT /api/v2/symbol`, `GET /api/download/symbols/...`

### Middleware

- **`OperationCancelledMiddleware`** converts `OperationCanceledException` to HTTP 409 Conflict.
- **`ConfigureBaGetterServer`** (implements multiple `IConfigureOptions<T>`) configures CORS, `FormOptions` (multipart upload limits via `MaxPackageSizeGiB`), forwarded headers, and IIS options.

## Configuration

Options class: `BaGetterOptions` in `src/BaGetter.Core/Configuration/`. Config sources (in order): `appsettings.json`, env vars, user secrets, Docker secrets (`/run/secrets/`), optional `BAGET_CONFIG_ROOT` env var.

`ValidateBaGetterOptions` validates all config at startup — database/storage/search types against whitelists, Entra config completeness when required, and numeric minimums (token expiry, failed attempts, lockout).

Top-level `BaGetterOptions` keys (see `src/BaGetter/appsettings.json`):

- `ApiKey`, `PackageDeletionBehavior` (`Unlist`/`HardDelete`), `AllowPackageOverwrites`, `MaxPackageSizeGiB` (default 8).
- `Database` — `Type` + `ConnectionString`.
- `Storage` — `Type` + provider-specific settings (`Path` for FileSystem).
- `Search` — `Type` (`Database` default; Azure Search alternative).
- `Mirror` — upstream read-through cache; `Enabled`, `PackageSource`, optional `Legacy` flag for NuGet v2.
- `Authentication` — see Authentication section. Legacy `Credentials[]` and `ApiKeys[]` arrays are supported for backward-compat alongside the new Local/Entra/Hybrid modes.
- `HealthCheck.Path` (default `/health`).
- `Statistics.EnableStatisticsPage`, `Statistics.ListConfiguredServices`.
- Standard ASP.NET `Kestrel` and `Logging` sections.

Docker defaults (`Dockerfile`): `Storage__Path=/data`, `Search__Type=Database`, `Database__Type=Sqlite`, `Database__ConnectionString=Data Source=/data/db/bagetter.db`. Volume-mount `/data` to persist packages, symbols, and the SQLite DB.

## Code Style

Enforced via `.editorconfig`: 4-space indent (2-space for JSON), PascalCase for public APIs/constants, `_camelCase` for private fields, `var` preferred, System usings first, all usings outside namespace.

Additional enforced rules (warning severity):
- File-scoped namespaces (`namespace Foo;`)
- Accessibility modifiers always required
- Readonly fields where possible
- No `this.` qualification
- Predefined types (`int`, `string`) over framework types (`Int32`, `String`)
- No primary constructors (`csharp_style_prefer_primary_constructors = false`)
- No expression-bodied methods or constructors (properties/accessors OK)
- One top-level type per file — with a narrow exception. Never bundle unrelated types together just to save tool calls. Co-locating a small helper type with its primary owner is OK when the helper is only meaningful in that owner's context: a result enum for an interface (e.g. `enum PackageAddResult` beside `interface IPackageDatabase`), a response item beside its response wrapper (e.g. `PackageDependent` beside `DependentsResponse`), or tightly-coupled serialization models. Rule of thumb: if a reader would look for the helper anywhere other than next to the primary type, split it out.

Suppress CS1591 for non-public XML doc comments.

## Testing

xUnit + Moq. Test projects mirror source: `tests/BaGetter.Core.Tests/`, `tests/BaGetter.Web.Tests/`, `tests/BaGetter.Protocol.Tests/`. Integration tests use in-memory SQLite.

Naming convention: the outer test class is named after the type under test — `<Type>Tests` in `BaGetter.Core.Tests`, `<Type>Facts` in `BaGetter.Web.Tests`. Tests for one method are grouped in a nested class named exactly after that method, with a shared-setup `FactsBase` (e.g. `PermissionServiceTests` → `public class CanPushAsync : FactsBase`). Some older `BaGetter.Web.Tests` classes are flat (no nested per-method classes); prefer the nested style for new tests. Do not use the older `The<Method>Method` idiom.

**`BaGetterApplication`** (WebApplicationFactory) is the integration test host — it mocks `SystemTime` to 2020-01-01, creates temp-dir SQLite databases, and wires up `XunitLoggerProvider`. Use `BaGetWebApplicationFactoryExtensions` helpers (`AddPackageAsync`, `AddSymbolPackageAsync`) to seed test data. Override config via in-memory dictionary config sources in test setup.

## Centralized Package Versions

All NuGet package versions managed in `Directory.Packages.props` at repo root. Do not specify versions in individual `.csproj` files.
