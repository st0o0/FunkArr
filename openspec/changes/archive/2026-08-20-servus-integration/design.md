## Context

FunkArr uses Servus 0.34.0 / Servus.Akka 0.3.14 but only for the AppBuilder startup
pattern (`IServiceSetupContainer`, `ActorSystemSetupContainer`,
`ApplicationSetupContainer<T>`). All runtime actor wiring is manual:

- **Setup**: `FunkArrActorSystemSetup.BuildSystem` pulls 5 services via
  `GetRequiredService`, manually creates `Props.Create(() => new Actor(...))`, calls
  `system.ActorOf`, and registers each ref in the `IActorRegistry`.
- **Actor → Actor**: `SearchActor` receives `IActorRef _ruleSetRegistry` and
  `_matchLedger` via constructor. These refs become stale if the target restarts under
  a `BackoffSupervisor`.
- **API → Actor**: Endpoints inject `IRequiredActor<T>` (Akka.Hosting) and unwrap
  `.ActorRef`.
- **Tests**: Raw `ActorSystem.Create("test")` with manual lifecycle. No test probes,
  no `ActorRegistry`, no Akka.Hosting.TestKit (packages referenced but unused).

The reference project Njord uses Servus.Akka's full runtime utilities and serves as the
target pattern.

## Goals / Non-Goals

**Goals:**

- Align actor registration, resolution, and testing patterns with Njord
- Eliminate stale-ref risk from constructor-injected `IActorRef` values
- Simplify `FunkArrActorSystemSetup` by removing manual DI and Props wiring
- Establish Akka.Hosting.TestKit as the standard for actor tests
- Populate `FunkArr.Tests.Shared` with reusable test infrastructure

**Non-Goals:**

- Changing external API behavior (Newznab, SABnzbd, Match Intelligence endpoints)
- Modifying persistence format or event DTOs
- Introducing `StreamConsumerActor` base class (Njord-specific, not needed yet)
- Introducing traced messaging (`TellTraced`, `AskTraced`) — not used in Njord either
- Renaming test files from `*Tests.cs` to `*Spec.cs` (cosmetic, not worth the churn)

## Decisions

### D1: WithResolvableActors for non-supervised actors

**Decision**: Register `SearchActor`, `RuleSetRegistryActor`, and `MatchLedgerActor`
via `WithResolvableActors`. Keep `DownloadQueueActor` in `WithActors` with
`BackoffSupervisor`.

**Rationale**: `WithResolvableActors` handles DI resolution and actor creation
automatically. Actors registered this way are discoverable via
`Context.GetActorAsync<T>()`. The `DownloadQueueActor` needs explicit
`BackoffSupervisor` wrapping which `WithResolvableActors` doesn't support — so it
stays in `WithActors` with a `RegisterWithBackoff<T>` helper (Njord pattern).

**Alternative considered**: Register everything via `WithActors`. Rejected because it
keeps the manual Props/DI boilerplate and doesn't enable `GetActorAsync`.

### D2: Context.GetActorAsync for actor-to-actor resolution

**Decision**: `SearchActor` resolves `RuleSetRegistryActor` and `MatchLedgerActor` via
`Context.GetActorAsync<T>().PipeTo(Self)` in `PreStart`.

**Rationale**: Async resolution with PipeTo is the Njord standard. It handles startup
ordering (actor may not exist yet) and enables re-resolution after `Terminated`.

**Design**: `SearchActor` gains a two-phase lifecycle:
1. **Resolving phase**: Calls `GetActorAsync` for both dependencies, stashes incoming
   search requests.
2. **Ready phase**: Both refs resolved, unstash, process normally. Watches both refs
   and re-resolves on `Terminated`.

This mirrors Njord's `StreamConsumerActor` pattern but without the base class (not
needed for a single actor). If more actors need this pattern later, extract a base
class then.

### D3: ActorRegistry.Get<T>() in endpoints

**Decision**: Inject `ActorRegistry` directly and call `.Get<T>()` instead of
`IRequiredActor<T>`.

**Rationale**: Consistency with Njord. `ActorRegistry.Get<T>()` is more concise and
doesn't require the `.ActorRef` unwrap. Both are synchronous and equivalent in
behavior.

### D4: ResolveChildActor for RuleSetGeneratorActor

**Decision**: `RuleSetRegistryActor` creates `RuleSetGeneratorActor` children via
`Context.ResolveChildActor<T>()` instead of `Context.ActorOf(Props.Create(...))`.

**Rationale**: DI-resolved child creation means the child actor's dependencies come
from the container, not from manual parameter passing. Currently
`RuleSetGeneratorActor` receives its deps via constructor from the parent — with
`ResolveChildActor`, it gets them from DI, decoupling parent from child's dependency
graph.

### D5: RegisterWithBackoff helper

**Decision**: Extract a static `RegisterWithBackoff<TActor>` method in
`FunkArrActorSystemSetup` for `DownloadQueueActor` (matching Njord's pattern).

**Rationale**: Encapsulates the BackoffSupervisor + registry boilerplate. If more
persistent actors are added later, the helper is ready.

### D6: Akka.Hosting.TestKit for all actor tests

**Decision**: Migrate `DownloadQueueActorTests`, `MatchLedgerActorTests`,
`RuleSetRegistryActorTests`, and `SearchActorTests` to extend
`Akka.Hosting.TestKit.TestKit`.

**Rationale**: Proper test lifecycle management, `ActorRegistry` access, test probes
via `CreateTestProbe()`, and `ConfigureAkka` override for registration — matching
Njord's test patterns. The packages are already referenced.

**Shared infrastructure**: Add `TestPersistenceConfig.AddTestPersistence()` extension
to `FunkArr.Tests.Shared` (same as Njord) for in-memory journal/snapshot in tests.

## Risks / Trade-offs

**[SearchActor becomes more complex]** → The two-phase resolve/ready lifecycle adds
code. Mitigated by: the pattern is well-proven in Njord, and the stale-ref risk it
eliminates is a real production concern with `BackoffSupervisor`.

**[Test rewrites are labor-intensive]** → Four test classes need significant changes.
Mitigated by: test behavior stays the same, only the infrastructure changes. The new
pattern is more maintainable long-term.

**[RuleSetGeneratorActor DI registration]** → Moving from parent-constructed to
DI-resolved means the actor and its dependencies must be registered in the container.
Currently `RuleSetGeneratorActor` takes an `HttpClient`, `MediathekClient`,
`TvdbClient`, and `FunkArrOptions` — all already in DI. Low risk.

## Migration Plan

1. No migration needed — this is an internal refactor with no external API changes.
2. All changes are backwards-compatible at the persistence level (no event format
   changes).
3. Rollback: revert the commits. No data migration involved.
