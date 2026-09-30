## Why

Actors carry explicit `IActorRef ReplyTo` fields in messages and state where Akka's built-in `Sender` propagation already provides the same routing. `PipeTo(Self, Sender)` preserves the original sender through multi-hop pipelines, and `Tell(msg, Sender)` forwards sender identity through routers — making the manual `ReplyTo` plumbing redundant in 4 of 5 call sites. Removing it simplifies messages, reduces actor state, and makes the routing model idiomatic.

## What Changes

- **TvSearchWorker / MovieSearchWorker**: Remove `IActorRef ReplyTo` from `SearchContext`. Replace `_context.ReplyTo.Tell(...)` with `Sender.Tell(...)` in all reply paths (completion, failure, unscored fallback).
- **MediathekViewWebManager**: Add sender argument to `.PipeTo(self, replyTo)` in `ExecuteQuery`. Remove `IActorRef ReplyTo` from `HttpCompleted` and `HttpFailed` internal messages. Use `Sender.Tell(...)` in handlers.
- **MatchMagicManager → MatchMagicActor**: Change `_router.Tell(new ExecuteScoring(config, msg.Items, Sender))` to `_router.Tell(new ExecuteScoring(config, msg.Items), Sender)`. Remove `IActorRef ReplyTo` from `ExecuteScoring` record. Use `Sender.Tell(...)` in `MatchMagicActor`.
- **No change**: `SearchGatewayManager.PendingSearch.OriginalSender` stays — scatter-gather aggregation genuinely requires stored sender identity.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `parallel-scoring`: `ExecuteScoring` message loses its `ReplyTo` field — the scoring pool uses `Sender` propagation instead.

## Impact

- **FunkArr.Search**: `TvSearchWorker`, `MovieSearchWorker`, `MediathekViewWebManager` — internal state/message changes only, no public API changes.
- **FunkArr.MatchMagic**: `ExecuteScoring` record simplified, `MatchMagicActor` and `MatchMagicManager` updated. `ExecuteScoring` is internal so no cross-domain impact.
- **Tests**: `MatchMagicActorTests`, `MovieSearchWorkerTests`, `TvSearchWorkerTests` need updates to remove `ReplyTo` construction from test messages.
