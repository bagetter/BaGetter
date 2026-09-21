# Repository Guidelines

## Project Structure & Module Organization
- `src/` contains all production projects. Key modules are `src/BaGetter` (host app), `src/BaGetter.Core` (domain/services), `src/BaGetter.Web` (NuGet API + UI), and `src/BaGetter.Protocol` (client SDK).
- Provider-specific integrations live in `src/BaGetter.Database.*` and `src/BaGetter.{Aws,Azure,Gcp,Aliyun,Tencent}`.
- `tests/` mirrors production areas: `BaGetter.Tests` (integration), `BaGetter.Core.Tests`, `BaGetter.Protocol.Tests`, and `BaGetter.Web.Tests`.
- `docs/` is the Docusaurus documentation site; `deployment templates/` contains Helm chart assets.

## Build, Test, and Development Commands
- `dotnet restore` restores solution dependencies.
- `dotnet build --no-restore` compiles all projects (same pattern as CI).
- `dotnet test --no-build --verbosity normal` runs all tests.
- `dotnet run --project src/BaGetter` starts the local server.
- `docker build -t bagetter:local .` builds the container image.
- Docs site (from `docs/`): `yarn`, `yarn start`, `yarn build`.

## Coding Style & Naming Conventions
- Follow `.editorconfig`: UTF-8, CRLF, trim trailing whitespace, final newline.
- Indentation: 4 spaces for C# and most files; 2 spaces for JSON/YAML/XML/web assets.
- C# conventions enforced by analyzers: file-scoped namespaces, braces preferred, `var` preferred when reasonable, `using` outside namespace.
- Naming: use PascalCase for types, members, and `const` fields.

## Testing Guidelines
- Test stack: xUnit + `Microsoft.NET.Test.Sdk` + `coverlet.collector` (centrally managed in `Directory.Packages.props`).
- Name test files by subject with `Tests`/`Facts` suffix (for example, `PackageServiceTests.cs`, `IndexModelFacts.cs`).
- Keep integration scenarios in `tests/BaGetter.Tests` and focused unit tests in project-specific test folders.

## Commit & Pull Request Guidelines
- Prefer Conventional Commit-style prefixes already used in history: `fix:`, `docs:`, `chore:`, `feat:`.
- Keep commit messages imperative and scoped to a single change.
- PRs should include: clear summary, linked issue (`#123` when applicable), test evidence (`dotnet test` output), and screenshots for UI/documentation updates.
- Ensure CI passes on both Linux and Windows runners before merge.

## Security & Configuration Tips
- Do not commit secrets; use environment variables and local `appsettings.*` overrides.
- Keep `nuget.config` and feed credentials out of source control.
- LDAP `BindPassword` must remain a retrievable secret, not a one-way hash. It should be supplied through environment variables, secret files, or another secret store because LDAP bind requires the original password at runtime.
- LDAP uses `System.DirectoryServices.Protocols`, which is cross-platform. When changing LDAP behavior, preserve non-Windows compatibility and document any OS-level LDAPS certificate trust requirements for Linux/macOS deployments.

## MCP & Documentation Lookup
- Prefer MCP Context7 when the user asks about library/framework docs, API usage, or code examples.
- Use Context7 proactively when it can improve solution quality or reduce ambiguity, even if not explicitly requested.
- Resolve the library ID first, then query docs with focused questions.
- Treat Context7 as the primary source for usage guidance before falling back to memory.
