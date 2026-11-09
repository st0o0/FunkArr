## Context

The Pathfinder actor state pattern is documented in `actor-state-management`
spec and CLAUDE.md. Newer states follow it cleanly, but older states
(Download, Feed-Crawl, RuleSet) predate the spec and use public setters with
direct actor mutation. The `resolve-child-actor-configure` spec already added
the `ResolveChildActor<T>(configure)` overload to Terp, but two actors still
use raw Props.

## Goals / Non-Goals

**Goals:**

- Every state property uses `{ get; private set; }` — no public setters.
- Every state mutation from actors goes through an `Apply()` method.
- Infrastructure flags that aren't domain state move to actor fields.
- Child pool creation uses `ResolveChildActor<T>()` consistently.
- Zero behavior changes — purely mechanical refactor.

**Non-Goals:**

- Changing state from `sealed class` to `record` or other structural changes.
- Adding `ProcessCommand` to states that don't have it (that's a separate
  concern for persistent actors).
- Refactoring states that already follow the pattern (the 10 clean states).
- Adding new tests — existing tests should continue to pass unchanged.

## Decisions

### D1: `{ get; private set; }` not `{ get; init; }`

**Choice**: Use `{ get; private set; }` for all state properties.

**Why not `init`**: State classes use mutable in-place mutation through
`Apply()` — `init` only allows setting during construction, which breaks
the Apply pattern. `private set` allows internal mutation while preventing
external access.

### D2: Infrastructure flags move to actor, not wrapped in Apply

**Choice**: `FeedCrawlManagerState.Running` and `.ResultReceived` become
private fields on `FeedCrawlManager` (the actor), not properties on the
state class.

**Why**: These flags track in-flight actor lifecycle (whether a crawl worker
is currently running), not domain state. They are not persisted, not
snapshotted, and not meaningful for queries. They belong on the actor as
infrastructure refs (per CLAUDE.md: "Infrastructure refs may stay on the
actor").

### D3: RuleSetWorkerState — add Apply methods

**Choice**: Add `Apply()` methods to `RuleSetWorkerState` for the two
properties (`Merged`, `Paths`) that are currently set directly by the actor.

**Why**: Even though `RuleSetWorkerState` is simple (2 properties, not
persistent), the pattern should be consistent. The Apply methods will be
trivially small but enforce the contract.

### D4: DownloadWorkerState — consolidate direct mutations into Apply

**Choice**: For `DownloadWorkerState`, where the actor sets `Progress`,
`Phase`, `Status`, `Media` directly from ~5 locations, introduce focused
`Apply()` overloads that accept the appropriate event/command/value types.

**Why**: The actor has existing Apply methods but bypasses them for certain
fields. Rather than creating one monolithic Apply, use targeted overloads
matching the event that triggers the mutation (consistent with the existing
pattern where `Apply` is overloaded by parameter type).

### D5: Terp ResolveChildActor for pool creation

**Choice**: Replace `Props.Create<ScoringActor>().WithRouter(...)` +
`Context.ActorOf()` in `ScoringManager` and the equivalent in
`FeedCrawlWorker` with `Context.ResolveChildActor<T>(props => props.WithRouter(...))`.

**Why**: Consistency with `EnrichmentManager` which already uses this pattern.
Neither `ScoringActor`, `FeedPageActor`, nor `FeedEvalActor` have DI
dependencies today, but the Terp extension works the same way and future DI
deps will just work.

## Risks / Trade-offs

- **[Wide touch, low risk]** → ~20 files across 5 domains, but purely
  mechanical (access modifier + method extraction). No logic changes.
- **[New Apply method signatures]** → Existing tests call Apply directly in
  some cases. Those calls continue to work as the methods are public.
- **[FeedCrawlWorker raw Props for runtime args]** → `FeedCrawlManager`
  creates `FeedCrawlWorker` with `Props.Create(() => new FeedCrawlWorker(input, timeProvider))`.
  This is legitimate (runtime args), NOT part of this change.
