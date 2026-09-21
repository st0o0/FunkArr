# CLAUDE.md

@AGENTS.md

## Workflow

Changes go through OpenSpec: `/opsx:explore` to think - `/opsx:propose` to create
a change (proposal/design/specs/tasks) - `/opsx:apply` to implement - `/opsx:archive`.

## Skill routing

### Project skills (FunkArr-specific)

- New actor: `funkarr-actor` (Pathfinder pattern with FunkArr namespaces)
- New message: `funkarr-message` (commands, queries, events)
- New endpoint: `funkarr-endpoint` (Minimal API + actor Ask)
- New test: `funkarr-test` (TestKit with DataPaths, TestDataFiles)

### Akka.NET skills (akka-skills plugin)

- Actor state pattern: `akka-skills:actor-state` (state records, Apply/GetSnapshot, persistence)
- Persistence separation: `akka-skills:persistence` (three-tier state model, SaveSnapshot, extend-only DTOs)
- Project structure: `akka-skills:project-structure` (solution layout, domain isolation, ArchUnitNET)
- Message conventions: `akka-skills:messages` (VerbNoun commands, QueryNoun queries) — this project uses Pattern A (dedicated Messages project)
- Actor testing: `akka-skills:testing` (Classic + Hosting TestKit, async assertions, persistence testing)
- Logging: `akka-skills:logging` (ILoggingAdapter in actors, ILogger in services, Serilog setup)
- Setup containers: `akka-skills:setup-container` (Servus AppBuilder, DI/Actor/App composition)
- Cluster hosting: `akka-skills:cluster-hosting` (Singletons, ShardRegions, MessageExtractor)
- Actor pools: `akka-skills:actor-pools` (Router-Pools, DI-Pools, Stash-Capacity)
- Persistence setup: `akka-skills:persistence-setup` (WithSqlPersistence, Provider, Clustering)
- Become state machines: `akka-skills:become-state-machines` (multi-phase workflows, stash-during-init, connection lifecycle)
- Advanced patterns: `akka-skills:advanced-patterns` (IWithTimers, ReceiveAsync, PipeTo, DeathWatch, Passivation, PersistAll)
- Streams: `akka-skills:streams` (Source/Flow/Sink, MergeHub/BroadcastHub, StreamRefs, custom GraphStage, supervision)
- Supervision: `akka-skills:supervision` (BackoffSupervisor, custom SupervisorStrategy, escalation)

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
