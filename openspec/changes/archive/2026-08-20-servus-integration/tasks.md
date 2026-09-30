## 1. Setup Container Refactor

- [x] 1.1 Add `RegisterWithBackoff<TActor>` static helper method to `FunkArrActorSystemSetup` (BackoffSupervisor + registry pattern from Njord)
- [x] 1.2 Move `DownloadQueueActor` registration to use `RegisterWithBackoff<DownloadQueueActor>` helper
- [x] 1.3 Register `RuleSetRegistryActor`, `MatchLedgerActor`, `SearchActor` via `WithResolvableActors` — remove manual `Props.Create` + `system.ActorOf` + `GetRequiredService` calls for these actors
- [x] 1.4 Remove all manual `serviceProvider.GetRequiredService` calls from `BuildSystem` that were used for actor construction
- [x] 1.5 Verify the application starts and actors are created correctly (`dotnet build` + smoke test)

## 2. Actor → Actor Resolution

- [x] 2.1 Refactor `SearchActor` to resolve `RuleSetRegistryActor` and `MatchLedgerActor` via `Context.GetActorAsync<T>().PipeTo(Self)` in `PreStart` — remove `IActorRef` constructor parameters
- [x] 2.2 Add resolving/ready two-phase lifecycle to `SearchActor`: stash search requests until both refs are resolved, then `BecomeReady` and unstash
- [x] 2.3 Add `Terminated` watch + re-resolve logic to `SearchActor` — on `Terminated`, transition back to resolving phase and re-initiate `GetActorAsync`
- [x] 2.4 Verify `SearchActor` correctly resolves dependencies and handles search requests end-to-end

## 3. Child Actor Creation

- [x] 3.1 Register `RuleSetGeneratorActor` for DI resolution (ensure its dependencies are in the container)
- [x] 3.2 Refactor `RuleSetRegistryActor` to create `RuleSetGeneratorActor` children via `Context.ResolveChildActor<RuleSetGeneratorActor>(name, args)` instead of `Context.ActorOf(Props.Create(...))`
- [x] 3.3 Verify rule set generation still works correctly

## 4. API Endpoint Resolution

- [x] 4.1 Refactor `NewznabEndpoints` to inject `ActorRegistry` and use `actorRegistry.Get<SearchActor>()` instead of `IRequiredActor<SearchActor>`
- [x] 4.2 Refactor `SabnzbdEndpoints` to inject `ActorRegistry` and use `actorRegistry.Get<DownloadQueueActor>()` instead of `IRequiredActor<DownloadQueueActor>`
- [x] 4.3 Refactor `MatchIntelligenceEndpoints` to inject `ActorRegistry` and use `actorRegistry.Get<MatchLedgerActor>()` instead of `IRequiredActor<MatchLedgerActor>`
- [x] 4.4 Remove all `using Akka.Hosting;` imports that are no longer needed (replaced by Servus.Akka or ActorRegistry) — `Akka.Hosting` still needed for `ActorRegistry`

## 5. Test Infrastructure

- [x] 5.1 Add `TestPersistenceConfig.cs` to `FunkArr.Tests.Shared` with `AddTestPersistence()` extension method (in-memory journal + snapshot store)
- [x] 5.2 Verify `FunkArr.Tests.Shared` project references are correct (`Akka.Persistence.Hosting`, `Akka.Hosting.TestKit`)

## 6. Actor Test Migration

- [x] 6.1 Migrate `DownloadQueueActorTests` to `Akka.Hosting.TestKit.TestKit` base class — N/A, tests events/DTOs only, no ActorSystem usage
- [x] 6.2 Migrate `MatchLedgerActorTests` to `Akka.Hosting.TestKit.TestKit` base class
- [x] 6.3 Migrate `RuleSetRegistryActorTests` to `Akka.Hosting.TestKit.TestKit` base class
- [x] 6.4 Migrate `SearchActorTests` to `Akka.Hosting.TestKit.TestKit` base class — N/A, tests query models only, no ActorSystem usage
- [x] 6.5 Remove manual `ActorSystem.Create("test")` / `Terminate()` / `IDisposable` / `IAsyncDisposable` patterns from all migrated tests

## 7. Verification

- [x] 7.1 Run full test suite (`dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj`) — 246/247 pass, 1 pre-existing failure (QualityProbeServiceTests.ProbeAsync_CachesResult)
- [x] 7.2 Run `dotnet build FunkArr.slnx` — 0 errors, 1 pre-existing warning (AK1004 ScheduleTellRepeatedly)
- [x] 7.3 Verify no remaining `IRequiredActor<T>` usages in the codebase
- [x] 7.4 Verify no remaining `ActorSystem.Create("test")` usages in tests
