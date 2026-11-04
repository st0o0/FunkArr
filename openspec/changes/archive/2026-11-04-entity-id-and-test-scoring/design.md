## Context

The `ScoringManager` is a non-persistent Cluster Singleton with three responsibilities:
1. Holding `MatchingConfig` state for production scoring
2. Routing `ScoreItems` to a `SmallestMailboxPool` of `ScoringActor` children
3. Handling `TestScoreWithEnrichment` — an async, multi-step orchestration (score via pool → enrich via EnrichmentManager → reply) that uses `ReceiveAsync` and blocks the singleton's mailbox

Responsibility 3 is ~120 lines of code unrelated to the manager's core job. It blocks the singleton during enrichment timeouts and mixes request-scoped orchestration with long-lived state management.

Separately, the entity ID interfaces (`IWithDownloadId`, `IWithSearchId`, `IWithRuleSetId`) are standalone types that don't integrate with Terp's `IWithEntityId` (routing key) or `IWithId` (Guid identity). This forces a hand-maintained `ShardMessageExtractor` with per-type pattern matching. The History domain uses `IWithRuleSetId` for its shard region, violating the one-interface-per-region convention.

## Goals / Non-Goals

**Goals:**
- Extract test-score orchestration into a per-request `TestScoringWorker` using Terp's `WithEntityRegion`
- Unify entity ID interfaces under Terp's `IWithEntityId` / `IWithId` hierarchy
- Enforce one `IWith*Id` per region by introducing `IWithHistoryId`
- Simplify `ShardMessageExtractor` to a single `IWithEntityId` case
- Keep the scoring pool in `ScoringManager` — the `TestScoringWorker` sends scoring requests to the manager

**Non-Goals:**
- Migrating existing ShardRegions to LocalEntityRegion (they work fine as cluster-sharded)
- Changing the REST API contract (request/response shapes stay the same)
- Refactoring the enrichment logic itself (just moving it)
- Introducing `IWithHistoryId` everywhere retroactively beyond History messages

## Decisions

### Decision 1: TestScoringWorker sends scoring to ScoringManager, not directly to the pool

The scoring router pool is a child of `ScoringManager`. Rather than extracting the pool into its own registry entry or passing `IActorRef` around, the `TestScoringWorker` resolves `IScoringManager` from the registry and sends `TestScoreItems` to it. The manager's existing one-liner forwards to the pool.

**Why over direct pool access:** The pool is an implementation detail of the manager. Exposing it would create a second way to reach scoring, and pool lifecycle is the manager's responsibility.

**Why over creating a separate pool in the worker:** Wasteful duplication — the pool already exists and is sized via `ScoringOptions.PoolSize`.

### Decision 2: LocalEntityRegion via Terp's WithEntityRegion, not ad-hoc Context.ActorOf

The `TestScoringWorker` is registered as `WithEntityRegion<ITestScoringRegion>` with `EntityIdExtractor`. This gives us:
- Automatic passivation (safety net for stuck requests)
- Metrics via `EntityRegionMetrics` (active entities, passivations)
- `MaxEntities` cap to limit concurrent test-score requests
- Same protocol as cluster sharding — migration path if needed later

**Why not just `Context.ActorOf` in the manager:** No passivation safety net, no metrics, no entity cap. The manager would need custom child lifecycle management.

### Decision 3: IWith*Id inherits IWithEntityId via default interface implementations

Each `IWith*Id` interface adds `: IWithEntityId` (and `: IWithId` for Guid-based IDs) with explicit default implementations of `EntityId` and `Id`. This is purely additive — message records don't change.

```
IWithDownloadId : IWithId, IWithEntityId
    Guid DownloadId { get; }
    Guid IWithId.Id => DownloadId;
    string IWithEntityId.EntityId => DownloadId.ToString();

IWithRuleSetId : IWithEntityId
    string RuleSetId { get; }
    string IWithEntityId.EntityId => RuleSetId;
```

**Why explicit interface implementation:** `Id` and `EntityId` are invisible in normal code (only accessible via cast). The domain-specific property (`DownloadId`, `SearchId`) stays the only visible member.

**Why not a base class or wrapper:** Interfaces with default methods are zero-cost, compose freely with records, and don't force a type hierarchy.

### Decision 4: Separate IWithHistoryId for History messages

History messages switch from `IWithRuleSetId` to a new `IWithHistoryId : IWithEntityId`. The property is `string HistoryId` — the value is still a ruleset ID string, but the interface is a separate routing contract.

**Why:** One interface per region is the convention. `IWithRuleSetId` is `RuleSetWorker`'s routing contract. History having its own interface makes it clear which region a message targets, and prevents accidental cross-routing when the `ShardMessageExtractor` moves to a single `IWithEntityId` case.

### Decision 5: ShardMessageExtractor reduces to single IWithEntityId case

```csharp
public override string EntityId(object message) => message switch
{
    IWithEntityId m => m.EntityId,
    _ => throw new ArgumentException(...)
};
```

This works because all `IWith*Id` interfaces now inherit `IWithEntityId`. No per-type discrimination needed — the extractor just reads the common property. The shard region already knows which entity type it manages because messages are sent to a specific `IActorRef` (the region).

### Decision 6: TestScoringWorker self-passivates after reply

After sending the response to the original sender, the worker calls `Context.Parent.Tell(new Passivate(PoisonPill.Instance))`. This is the same passivation protocol that cluster sharding uses. `EntityRegionOptions.PassivationTimeout` (30s) acts as a safety net for workers that hang during enrichment.

The worker does NOT use `ReceiveAsync`. It uses `PipeTo` for the scoring Ask and enrichment Ask, with `Become` for state transitions:

```
WaitingForScore → ScoreCompleted → (if enrichment) WaitingForEnrichment → Reply + Passivate
                                   (if no enrichment) → Reply + Passivate
```

## Risks / Trade-offs

- **[Extra hop for test scoring]** TestScoringWorker → ScoringManager → Pool → reply, instead of direct pool access. One additional message hop. → Acceptable: test scoring is an interactive debug tool, not a hot path. The indirection keeps the pool encapsulated.
- **[Default interface methods require .NET 5+]** → Non-issue: project targets net10.0.
- **[History messages change interface]** `IWithRuleSetId` → `IWithHistoryId` on 4 messages. → Low risk: the property name changes (`RuleSetId` → `HistoryId`) which is a compile-time catch. The value stays the same string.
- **[ShardMessageExtractor loses type discrimination]** A message sent to the wrong region would still extract an entity ID instead of throwing. → Mitigated: message routing is via typed `IActorRef` from the registry, not by extractor selection. Sending to the wrong region is already a programming error that wouldn't be caught by the extractor anyway.
