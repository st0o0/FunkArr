## Why

The `actor-state-management` spec requires state mutation through `Apply()`
methods and thin actors, but 10 of 20 actor state classes have public setters
that allow actors to bypass `Apply()` and mutate state directly. Six are
HIGH severity (actors actively use direct mutation), and one state
(`RuleSetWorkerState`) has no `Apply` methods at all. Additionally, two actors
use raw `Props.Create<T>()` instead of Terp's `ResolveChildActor<T>()` for
child pool creation.

## What Changes

- All state class properties change from `{ get; set; }` to
  `{ get; private set; }`, enforcing that only the state class itself can
  mutate its properties.
- Missing `Apply()` methods are added to states that lack them
  (`RuleSetWorkerState`, and others where actors currently set properties
  directly).
- Infrastructure flags (`FeedCrawlManagerState.Running`,
  `FeedCrawlManagerState.ResultReceived`) move from state to actor fields.
- Actor code that directly mutates state properties is refactored to call
  `Apply()` methods.
- `ScoringManager` and `FeedCrawlWorker` child pool creation migrated from
  `Props.Create<T>()` + `Context.ActorOf()` to `Context.ResolveChildActor<T>()`.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `actor-state-management`: Add requirement that state properties use
  `{ get; private set; }` access, and that ALL state mutation from actors
  goes exclusively through `Apply()` methods. Infrastructure-only fields that
  are not domain state must live on the actor, not in the state class.

## Impact

- **10 state classes** across Download, Feed, RuleSet, Search, and Scoring
  domains.
- **~10 actor classes** that directly mutate state properties need refactoring.
- **2 actor classes** (`ScoringManager`, `FeedCrawlWorker`) change child pool
  creation pattern.
- No persistence changes — this is a code discipline change, not a data change.
- No API contract changes.
