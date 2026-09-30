## Context

Persistent actors in FunkArr store mutable state as scattered private fields (e.g. `_title`, `_status`, `_category`) directly on the actor class. Apply methods that mutate these fields also live on the actor. This interleaves domain state logic with actor infrastructure concerns (message routing, persistence calls, logging, Become transitions).

Akka.Pathfinder solved this with dedicated state classes per actor, but used partial classes to split actors across 4-5 files. FunkArr already has some partial classes (ShowActor, MovieActor, RuleSetRegistryActor) for Events/Messages — this refactoring consolidates to a simpler two-file pattern.

Currently 7 persistent actors are affected: DownloadRequestActor, DownloadActor, QueueActor, ShowActor, MovieActor, RuleSetRegistryActor, RecentMatchActor.

## Goals / Non-Goals

**Goals:**
- Extract all mutable actor state into dedicated `*State` classes with `Apply(event)` methods
- Make actors thin orchestrators: message routing, persistence, side effects only
- Eliminate partial classes — each actor becomes one `.cs` file plus one `*State.cs` sibling
- Preserve all persistence wire formats, message contracts, and external behavior

**Non-Goals:**
- Refactoring non-persistent actors (too simple to benefit)
- Changing persistence strategy (event-sourced vs snapshot)
- Introducing a shared base class or interface for state objects
- Adding new tests for state objects (can be done later)
- Changing the Events or Persistence DTO patterns

## Decisions

### State class shape: `sealed class` with `private set` properties

State classes are mutable objects with controlled mutation via `Apply` methods. Using `sealed class` with `private set` properties over records-with-`with` because:
- Event-sourced actors apply events one at a time during replay — in-place mutation is natural
- `with` expressions would allocate a new object per event during recovery (hundreds of events)
- The state is never shared or compared by value — reference semantics are correct

### One state class per actor, no shared base

Each `*State` class is standalone. No `IActorState` interface or `ActorState<T>` base — the actors are too different (DownloadActor is a stage machine, ShowActor merges rulesets, QueueActor manages a linked list). A shared abstraction would be hollow.

### Messages stay nested on actor class

Messages (commands, queries, responses) remain as nested `sealed record` types on the actor class. They define the actor's public API and belong with the actor, not the state. Events stay in their existing `*Events.cs` files.

### Collapse partial classes for ShowActor, MovieActor, RuleSetRegistryActor

These currently split across `.cs`, `.Events.cs`, `.Messages.cs`. After extracting state, the main actor file is thin enough to hold nested messages inline. Events stay in their own file (they're referenced by persistence DTOs in a different namespace).

### Guard properties on state

Domain guards move from inline checks on the actor (e.g. `if (!string.IsNullOrEmpty(_title))`) to semantic properties on the state (e.g. `state.IsInitialized`). This makes the actor's intent clear and the guard testable.

### Snapshot conversion lives on state

For actors that use snapshots (ShowActor, MovieActor, RuleSetRegistryActor, RecentMatchActor), the state class provides `ToSnapshot()` and a static `FromSnapshot(...)` factory method. This keeps the state ↔ snapshot mapping co-located with the state it describes.

### Command handlers can inline as lambdas

When the actor is thin enough after state extraction, named `Handle*` methods can be replaced with inline lambdas in the `Ready()` method. This reduces boilerplate and keeps the routing visible in one place. For larger actors (DownloadActor, ShowActor), named methods may be retained for readability.

## Risks / Trade-offs

- **Larger state classes for complex actors** → DownloadActorState and ShowActorState will be substantial. Mitigated by each being a single cohesive responsibility (domain state), not a god class.
- **Migration of partial class content** → Merging `.Messages.cs` back into the actor could create merge conflicts with in-flight branches. Mitigated by doing this on a clean main branch with no concurrent work.
- **Recovery correctness** → Moving Apply methods to the state class changes call sites from `Apply(evt)` to `_state.Apply(evt)`. Mechanical refactoring, but any missed call would silently break recovery. Mitigated by existing test coverage and manual review.
