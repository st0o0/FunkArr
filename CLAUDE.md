# CLAUDE.md

@AGENTS.md

## Workflow

Changes go through OpenSpec: `/opsx:explore` to think - `/opsx:propose` to create
a change (proposal/design/specs/tasks) - `/opsx:apply` to implement - `/opsx:archive`.

## Skill routing

### Project skills (FunkArr-specific)

- `funkarr-actor` -- **Load before creating/modifying any actor.** Pathfinder
  pattern with FunkArr namespaces, Persist/DeferAsync/SaveSnapshot pattern.
- `funkarr-message` -- **Load before creating/modifying messages or persistence
  DTOs.** Commands, queries, events, three type worlds (Messages, Persistence, API).
- `funkarr-endpoint` -- **Load before creating/modifying API endpoints.** Minimal
  API + actor Ask, for FunkArr.Api only.
- `funkarr-test` -- **Load before writing tests.** TestKit with DataPaths,
  TestDataFiles.

### Akka.NET skills -- when to load

**Always load the relevant skill before writing Akka code.** The project skills
above are FunkArr-specific templates; the akka-skills below provide deep pattern
knowledge. Load both when applicable.

| Task | Load these skills |
|------|-------------------|
| New actor (any type) | `funkarr-actor` + `akka-skills:actor-state` |
| Actor with persistence | + `akka-skills:persistence` |
| Sharded entity / Singleton | + `akka-skills:cluster-hosting` |
| Actor with timers, PipeTo, DeathWatch | + `akka-skills:advanced-patterns` |
| Become/Unbecome state machine | + `akka-skills:become-state-machines` |
| Akka.Streams pipeline | + `akka-skills:streams` |
| Supervision strategy | + `akka-skills:supervision` |
| Actor pools / routing | + `akka-skills:actor-pools` |
| New message / command / query | `funkarr-message` + `akka-skills:messages` |
| Persistence DTOs / events | `funkarr-message` + `akka-skills:persistence` |
| Persistence infrastructure setup | `akka-skills:persistence-setup` |
| Actor tests | `funkarr-test` + `akka-skills:testing` |
| Servus setup containers | `akka-skills:setup-container` |
| Logging in actors | `akka-skills:logging` |
| API endpoints | `funkarr-endpoint` |
| Solution structure questions | `akka-skills:project-structure` |

### Plugin skills

- Akka.NET: `dotnet-skills:akka-*` (hosting, testing, best-practices, management)
- Actor patterns: `dotnet-skills:csharp-concurrency-patterns`
- C# coding: `dotnet-skills:csharp-coding-standards`, `dotnet-skills:csharp-type-design-performance`
- EF Core / DB: `dotnet-skills:efcore-patterns`, `dotnet-skills:database-performance`
- Testing: `dotnet-skills:akka-testing-patterns`, `dotnet-skills:testcontainers`, `dotnet-skills:snapshot-testing`
- Project structure: `dotnet-skills:project-structure`, `dotnet-skills:package-management`
- DI / Config: `dotnet-skills:microsoft-extensions-dependency-injection`, `dotnet-skills:microsoft-extensions-configuration`
- Aspire: `dotnet-skills:aspire-*`
- Serialization: `dotnet-skills:serialization`
- OpenTelemetry: `dotnet-skills:opentelementry-dotnet-instrumentation`
