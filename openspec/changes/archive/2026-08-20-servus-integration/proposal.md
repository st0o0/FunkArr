## Why

FunkArr uses Servus only for startup bootstrapping (AppBuilder pattern) but none of the
runtime actor utilities — typed resolution, async dependency lookup, DI-resolved child
creation. Njord (same author, same stack) has a clean Servus integration that gives
self-healing actor references, eliminates manual DI wiring in setup containers, and
enables proper Akka.Hosting.TestKit-based testing. Aligning FunkArr now — before the
actor hierarchy grows — avoids accumulating tech debt and stale-ref bugs.

## What Changes

- **Actor registration**: Non-supervised actors (`SearchActor`, `RuleSetRegistryActor`,
  `MatchLedgerActor`) move from manual `Props.Create` + `system.ActorOf` to
  `WithResolvableActors` with DI-resolved construction. Supervised actors
  (`DownloadQueueActor`) keep `WithActors` + `BackoffSupervisor` via a
  `RegisterWithBackoff` helper.
- **Actor → Actor resolution**: Replace raw `IActorRef` constructor injection with
  `Context.GetActorAsync<T>()` + `PipeTo(Self)` for async, self-healing references.
- **API → Actor resolution**: Replace `IRequiredActor<T>` with direct
  `ActorRegistry.Get<T>()` in all endpoint classes.
- **Child actor creation**: Replace `Context.ActorOf(Props.Create(...))` with
  `Context.ResolveChildActor<T>()` for DI-resolved child actors.
- **Setup container cleanup**: Remove manual `GetRequiredService` calls from
  `FunkArrActorSystemSetup` — DI resolution moves into actor constructors.
- **Test infrastructure**: Migrate actor tests from raw `ActorSystem.Create("test")` to
  `Akka.Hosting.TestKit.TestKit` base class with test probes, `ActorRegistry`, and
  in-memory persistence. Add `TestPersistenceConfig` helper to `FunkArr.Tests.Shared`.

## Capabilities

### New Capabilities

- `actor-resolution`: Typed actor resolution via Servus.Akka — registration with
  `WithResolvableActors`, runtime lookup with `Context.GetActorAsync<T>()` /
  `ActorRegistry.Get<T>()`, and DI-resolved child creation with
  `Context.ResolveChildActor<T>()`.
- `actor-test-infrastructure`: Akka.Hosting.TestKit-based test infrastructure — shared
  helpers (`TestPersistenceConfig`), test probes, `ActorRegistry` in tests.

### Modified Capabilities

_(No spec-level behavior changes — this is an internal refactor of how actors are wired
and tested, not a change to external APIs or domain behavior.)_

## Impact

- **Setup containers**: `FunkArrActorSystemSetup` rewritten — `WithResolvableActors`
  replaces manual actor creation for non-supervised actors.
- **Actor classes**: `SearchActor`, `RuleSetRegistryActor` gain async dependency
  resolution phase. Constructor signatures change (drop `IActorRef` params, add DI
  services directly).
- **Endpoint classes**: `NewznabEndpoints`, `SabnzbdEndpoints`,
  `MatchIntelligenceEndpoints` switch from `IRequiredActor<T>` to `ActorRegistry`.
- **Test project**: `DownloadQueueActorTests`, `MatchLedgerActorTests`,
  `RuleSetRegistryActorTests`, `SearchActorTests` rewritten on
  `Akka.Hosting.TestKit.TestKit`. `FunkArr.Tests.Shared` gets
  `TestPersistenceConfig.cs`.
- **No external API changes** — Newznab and SABnzbd surfaces remain identical.
- **No persistence changes** — event DTOs and journal format untouched.
