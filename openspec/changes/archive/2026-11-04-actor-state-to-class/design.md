## Context

Actor states are split between two patterns: 12 immutable records (with-expression Apply returning new state) and 5 mutable classes (void Apply). This inconsistency means developers must check which pattern a state uses before modifying it. Additionally, several actors keep mutable domain state as direct fields instead of in the state object, and FeedCrawlWorkerState has a correctness bug where a record mutates List fields in-place.

## Goals / Non-Goals

**Goals:**
- Single consistent pattern for all actor states: sealed class, void Apply, mutable collections
- All mutable domain state encapsulated in state objects (no leaked fields on actors)
- Consistent `Apply` naming (no prefixed variants)
- Fix FeedCrawlWorkerState shared-reference bug

**Non-Goals:**
- Changing persistence records (they remain sealed records in FunkArr.Persistence)
- Changing message types (they remain sealed records in FunkArr.Messages)
- Changing actor base classes or Akka patterns
- Performance optimization (this is a consistency refactor; perf improvement is a side effect)

## Decisions

### 1. Sealed class with void Apply (not fluent return)

**Choice**: `void Apply(Event e)` mutating in-place
**Over**: `State Apply(Event e)` returning new instance (fluent)

Rationale: Void Apply makes mutation explicit. No accidental "forgot to reassign" bugs. Actor code becomes `_state.Apply(e)` instead of `_state = _state.Apply(e)`. The Search domain already uses this pattern successfully.

### 2. Instance methods (not extension methods)

**Choice**: Instance methods on the state class
**Over**: Static extension methods on records (current pattern for record states)

Rationale: Extension methods were necessary because records are immutable and `Apply` needed to return a new instance. With mutable classes, instance methods are the natural choice — they have direct access to private fields and the `this` reference.

### 3. Mutable collections (List, Dictionary, HashSet)

**Choice**: Standard mutable collections
**Over**: ImmutableList, ImmutableDictionary, ImmutableHashSet

Rationale: Akka mailbox guarantees single-threaded access. Immutable collections add allocation overhead on every modification and provide no thread-safety benefit in this context. The FeedCrawlWorkerState bug proves that mixing mutable operations with immutable types is worse than honest mutability.

### 4. ProcessCommand returns persistence record only (not tuple)

**Choice**: `PersistedEvent ProcessCommand(Command cmd)` — returns only the persistence record
**Over**: `(State, PersistedEvent) ProcessCommand(Command cmd)` — current tuple pattern

Rationale: With mutable state, ProcessCommand should not return a new state. The actor calls ProcessCommand to get the persistence record, then calls Apply in the Persist callback. This separates validation/projection (ProcessCommand) from mutation (Apply).

### 5. Empty as static readonly field (unchanged)

Keep `public static readonly Empty` pattern. For classes, this returns `new XxxState()` with default-initialized collections.

### 6. Actors with no state type get one

MediathekViewWebManager, RuleSetWorker, FeedEvalActor, and TestScoringWorker currently have mutable state as direct actor fields. Each gets a proper state class following the same pattern.

## Risks / Trade-offs

**[Persistence compatibility]** → No risk. State classes are never serialized directly — only `Persisted*State` records go to the journal/snapshot store, and those remain unchanged as sealed records.

**[Large diff]** → Mitigated by doing it domain-by-domain. Each domain can be converted and tested independently. The pattern is mechanical.

**[Extension methods in tests]** → Tests that use `with`-expressions to set up state will need to switch to property assignment or Apply calls. Mechanical but touches many test files.

**[FromPersistence / FromSnapshot return type]** → These static factory methods currently return records. They'll return class instances instead. The signature stays the same, callers are unaffected.
