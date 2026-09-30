## Context

FunkArr was reset on 2026-08-30 — the entire `src/` directory was deleted after the
previous single-csproj architecture accumulated too much coupling and eroded code
quality. This change creates the structural foundation for a multi-project solution
that enforces domain isolation at the compiler level.

The reference project njord (D:\GIT\njord) uses the same tech stack and conventions.
Its build infrastructure (Directory.Build.props, Directory.Packages.props, global.json,
slnx) serves as the template, adapted for FunkArr's multi-project structure.

## Goals / Non-Goals

**Goals:**
- All 18 projects created with correct SDK types and inter-project references
- `dotnet build` succeeds on the empty solution
- `dotnet format --verify-no-changes` passes
- CI pipeline builds and validates formatting
- Dockerfile builds the host project
- Vue.js UI project scaffolded with Vite + Tailwind

**Non-Goals:**
- No business logic, no actors, no messages, no APIs
- No architecture tests yet (land with the slice that makes them pass)
- No Akka persistence configuration
- No end-to-end functionality

## Decisions

### 1. Follow njord's build infrastructure exactly

Use the same `global.json` (.NET SDK 10.0.102), `Directory.Build.props` patterns
(TargetFramework, ImplicitUsings, Nullable, test project detection by name),
and `Directory.Packages.props` (central package management with transitive pinning,
same Akka/Servus/Serilog/xUnit versions).

**Why over diverging:** njord is battle-tested by the same author. Consistent
versions across projects simplify maintenance and avoid version mismatch bugs.

### 2. .editorconfig with default .NET rules

Njord has no .editorconfig. FunkArr adds one with Microsoft's default .NET style rules
to enforce consistent formatting via `dotnet format`. Built-in analyzers only — no
StyleCop or SonarAnalyzer.

**Why built-in only:** fewer dependencies, less config churn, and the built-in rules
cover the important cases (naming, spacing, braces, usings).

### 3. Project reference graph — flat, no cross-domain

```
Messages, Persistence     → (no dependencies)
Core                       → Messages, Persistence
Search, Download, RuleSet  → Core
MatchMagic                 → Core
Api, IndexerApi, DownloadApi → Core
Host (FunkArr)             → all domain + adapter + infrastructure projects
```

Domain projects never reference each other. The host wires everything via DI and
Akka message routing.

**Why Core bundles Akka/Servus refs:** avoids every domain project declaring the
same 5 NuGet packages. Core is the "you're in the FunkArr actor system" package.

### 4. Test projects per domain with shared infrastructure

Each domain gets its own test project (e.g., `FunkArr.Search.Tests`). All share
`FunkArr.Tests.Shared` for fakes, builders, and test-kit helpers. Test projects
use `dotnet run` (Microsoft.Testing.Platform), not `dotnet test`.

**Why per-domain:** enforces the same isolation boundary in tests. A Search test
that accidentally needs Download types is a design smell.

### 5. Vue.js UI as a separate frontend project

`FunkArr.UI` lives under `src/` with its own `package.json`, Vite config, and
Tailwind setup. The host project serves the built assets from `wwwroot/`.

**Why Vite + Tailwind:** same as previous iteration, proven stack for this scale.

### 6. slnx format

Use the new `.slnx` format (XML-based, simpler than `.sln`). Njord already uses it.

## Risks / Trade-offs

- **[18 empty projects create noise]** → Each project gets only a csproj and possibly
  a placeholder class. No premature abstractions. Projects stay empty until their
  slice lands.
- **[Package version drift with njord]** → Pin versions in Directory.Packages.props.
  Dependabot handles updates. Both projects share the same Akka/Servus versions.
- **[UI project adds Node.js to the build chain]** → Dockerfile uses multi-stage
  build. CI runs frontend build separately. Backend developers don't need Node
  locally unless working on UI.
