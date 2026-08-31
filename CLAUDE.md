# CLAUDE.md

## Project

FunkArr — German public broadcaster media library integration for the *arr
ecosystem. A .NET service (Docker container) on Akka.NET: searches ARD/ZDF/etc.
Mediatheken via MediathekViewWeb API, downloads video + subtitles, remuxes to
MKV via FFmpeg, and exposes Newznab-compatible indexer API (for Sonarr/Radarr/
Prowlarr) and SABnzbd-compatible download client API.

## Solution structure

Multi-project solution with domain isolation:

```
src/
├── FunkArr.slnx
├── FunkArr/                        # Host: Program.cs, Startup, DI, Config
├── FunkArr.Core/                   # Common types, Akka/Servus refs
├── FunkArr.Api/                    # Internal REST API (JSON, OpenAPI-first)
├── FunkArr.ArrApi/                 # Newznab + SABnzbd adapter (thin translation)
├── FunkArr.Search/                 # MediathekViewWeb query + fetch
├── FunkArr.Download/               # Download pipeline, FFmpeg, subtitles, muxing
├── FunkArr.RuleSet/                # Ruleset registry, models, GitHub sync, generator
├── FunkArr.MatchMagic/             # Match scoring, stats, diagnostics
├── FunkArr.Messages/               # Commands, queries, responses (all domains)
├── FunkArr.Persistence/            # DTOs (extend-only, versioned)
├── FunkArr.UI/                     # Vue.js frontend (Vite + Tailwind)
├── FunkArr.Search.Tests/
├── FunkArr.Download.Tests/
├── FunkArr.RuleSet.Tests/
├── FunkArr.MatchMagic.Tests/
├── FunkArr.Api.Tests/
├── FunkArr.ArrApi.Tests/
└── FunkArr.Tests.Shared/           # Shared test infrastructure
```

### Architecture guardrails

- **Domain isolation.** Domain projects (Search, Download, RuleSet, MatchMagic)
  must not reference each other. Communication only through Messages.
- **Adapter projects are thin translators.** ArrApi contains no business logic
  — it translates between external wire formats (Newznab XML, SABnzbd JSON) and
  internal Messages.
- **Reference direction:** Host → Api/Adapters → Domains → Core → Messages/Persistence.
  Never upward, never cross-domain.
- **Core bundles framework refs.** FunkArr.Core references Messages + Persistence
  and declares Akka/Servus NuGet packages. Domain projects reference only Core.
- **Two API surfaces.** Newznab XML (indexer for Prowlarr) and SABnzbd JSON
  (download client for Sonarr/Radarr). Both authenticate via `ApiKey` query
  parameter. These are compatibility adapters — the internal API is modern
  JSON/OpenAPI-first.

### Actor naming

- `*Manager` — Cluster Singleton
- `*Worker` — Sharded Entity (ShardRegion)
- `*Actor` — everything else (local, child, transient)

### Persistence

- **No separate Event types.** Flow: Command (Message) → Actor processes →
  Persistence DTO written to journal → State updated.
- **Persistence DTOs** (`FunkArr.Persistence`): extend-only. Never remove or
  rename a `[JsonProperty]` string. New properties must be nullable or have a
  default. Increment `Version` when semantics change. Recovery code must handle
  all versions >= 1.
- **Persistence tiers.** T1 (critical, event-sourced): queue, downloads.
  T2 (cache warmth, event-sourced + snapshots): resolvers, match quality.
  T3 (ephemeral): everything else.

### Servus & Servus.Akka usage

Use:
- **AppBuilder startup** — three setup containers: `IServiceSetupContainer` (DI),
  `ActorSystemSetupContainer` (actors + persistence),
  `ApplicationSetupContainer` (endpoints).
- **Actor registration** — `WithResolvableActors(b => b.Register<T>("name"))`.
- **Actor resolution** — `context.GetActor<T>()`, `context.ResolveChildActor<T>()`.
- **Safe child communication** — `context.GetChild("name")` → `Option<IActorRef>`,
  `context.ChildTell()`, `context.ChildForward()`.
- **`Option<T>` extensions** — `Match(some, none)` pattern.
- **Diagnostics** — `ServusTrace.For("category")` for tracing.

Do NOT use:
- `HandlerRegistry` — use `Receive<T>()` directly.
- `LocalEntityRegion` — use Akka.NET Cluster Sharding.
- `ActorRef<T>` — use `IActorRef` + `IActorRegistry`.
- Concurrency utils (`NamedSemaphoreSlimStore`, etc.).

## Build & test

All commands run from `src/`:

```powershell
dotnet build FunkArr.slnx
dotnet format --verify-no-changes                                                          # style check
```

Tests are xUnit v3 on Microsoft.Testing.Platform — `dotnet run`, **not** `dotnet test`.
Each domain has its own test project. Run all or target a specific one:

```powershell
dotnet run --project FunkArr.Search.Tests/FunkArr.Search.Tests.csproj
dotnet run --project FunkArr.Download.Tests/FunkArr.Download.Tests.csproj
```

Run the service from `src/FunkArr/` (`dotnet run`). Configuration layers:
- `appsettings.json` — production defaults.
- `appsettings.Development.json` — dev overrides.
- Environment variables — Docker-compose (`FunkArr__ApiKey`, etc.).

## C# conventions

- `sealed` by default, `record` for messages/DTOs, nullable enabled everywhere.
- No XML docs. Code speaks through naming.
- `dotnet format` enforced — run after editing `.cs` files. CI rejects violations.
- Built-in .NET analyzers only (no StyleCop, no SonarAnalyzer).
- `.editorconfig` defines all style rules.

### Messages

- Nested `sealed record` inside owning actor (commands, queries, responses).
- Prefer primitive parameters. No external domain types in messages.
- Below ~10 parameters: flat record.
- Above ~10 parameters: one level of nesting allowed (nested record defined
  inside the message, used only by that message).

```csharp
// Good: flat, primitive
public sealed record StartDownload(string MediaId, string Url, int Quality);

// Good: >10 params, one nested level
public sealed record StartDownload(string MediaId, DownloadSpec Spec)
{
    public sealed record DownloadSpec(string Url, int Quality, ...);
}

// Forbidden: external domain types as parameters
// Forbidden: multiple nesting levels
```

### Actors

- Inherit `ReceiveActor` directly (no custom base classes).
- Actors own explicit state records — `sealed record State(...)`.
- Use `Receive<T>()` for message handling.
- Single-node Cluster Sharding for entity actors.

### Architecture tests

ArchUnitNET tests enforce:
- Reference direction (no cross-domain, no upward references).
- Naming conventions (`*Manager` = singleton, `*Worker` = sharded, `*Actor` = other).
- Messages/Persistence have no dependency on Akka.
- All domain types are `sealed`.

## Git & CI

- **NEVER `git push`** — the user pushes.
- Conventional Commits (commitlint-enforced). Single-line only, no Co-Authored-By.
- Versioning is release-please. Never edit `<Version>` in `Directory.Build.props`.
- Central package management: versions in `Directory.Packages.props` only.
- CI gates: `dotnet format --verify-no-changes` + tests + architecture tests.

## Workflow

Changes go through OpenSpec: `/opsx:explore` to think → `/opsx:propose` to create
a change (proposal/design/specs/tasks) → `/opsx:apply` to implement → `/opsx:archive`.

## Skill routing (invoke by name)

- Actors & supervision: `sepp:actor-pattern-library`, `sepp:resilience-patterns`
- Messages & pipeline design: `sepp:message-driven-designer`
- Domain modeling: `sepp:domain-modeling-patterns`
- Servus patterns: `servus-skills:servus-*` (startup, actors, etc.)

## References

- MediathekViewWeb API: https://mediathekviewweb.de/ (search endpoint)
- Newznab API spec: https://newznab.readthedocs.io/
- SABnzbd API spec: https://sabnzbd.org/wiki/advanced/api
