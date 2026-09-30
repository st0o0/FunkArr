## Why

Persistent actors store state as scattered private fields directly on the actor class, mixing domain state with actor infrastructure (message routing, persistence, logging). This makes actors hard to reason about, test state logic in isolation, and increases the risk of state inconsistencies during event replay. Extracting state into dedicated objects follows the pattern proven in Akka.Pathfinder and makes each actor's domain logic self-contained and independently verifiable.

## What Changes

- Extract all mutable state fields from each persistent actor into a dedicated `*State.cs` sibling class
- State classes own `Apply(event)` methods, guard properties (e.g. `IsInitialized`), and computed state
- Actors become thin orchestrators: message routing, persistence calls, side effects (logging, passivation)
- Collapse existing partial class files (`.Events.cs`, `.Messages.cs`) into the single actor file where actor size permits
- No partial classes after refactoring — each actor is one `.cs` file plus one `*State.cs` file
- Non-persistent actors (BrowseActor, MediathekGatewayActor, SearchRequestActor, RefreshActor, pipeline workers) are unchanged

## Capabilities

### New Capabilities

- `actor-state-pattern`: Documents the state object pattern as an architectural convention — state class structure, Apply methods, guard properties, relationship to persistence DTOs

### Modified Capabilities

No spec-level behavior changes. This is a pure internal refactoring — all actor APIs, message contracts, persistence formats, and external behavior remain identical.

## Impact

- **Code**: All 7 persistent actors refactored: DownloadRequestActor, DownloadActor, QueueActor, ShowActor, MovieActor, RuleSetRegistryActor, RecentMatchActor
- **Files**: 7 new `*State.cs` files created; 6 partial class files (`.Events.cs`, `.Messages.cs`) merged back into main actor files where appropriate
- **Tests**: Existing actor tests should pass without changes (behavior unchanged). New unit tests possible for state objects in isolation.
- **Persistence**: No changes to journal DTOs, snapshot records, or persistence IDs — wire format fully preserved
- **APIs**: No changes — all message contracts and external APIs unchanged
