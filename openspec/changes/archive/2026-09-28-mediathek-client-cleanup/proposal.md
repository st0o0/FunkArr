## Why

MediathekClient builds Akka messages (`QueryMediathekCompleted`) internally and caches them as-is, mixing HTTP infrastructure with actor messaging concerns. The MediathekViewWebManager's slot tracking is a blind counter with no request correlation, making it impossible to debug which requests are in-flight. The QueryBuilder (orchestration logic) lives in the Client instead of the Manager.

## What Changes

- MediathekClient becomes a pure HTTP+cache layer: accepts a JSON string, returns an internal `MediathekQueryResult` DTO, no message imports
- MediathekViewWebManager uses an internal Akka.Stream pipeline (`Source.ActorRef` -> `Select` -> `SelectAsyncUnordered(3)` -> `Sink.ActorRef`) instead of manual PipeTo + Stash
- QueryBuilder invocation moves from Client into the stream's `Select` stage
- Messages are constructed in the actor's `Receive<StreamSuccess>` handler, not in the Client
- Sender correlation via `Dictionary<Guid, IActorRef>` replaces the `MediathekViewWebManagerState` counter
- `IWithUnboundedStash` removed, buffering handled by `Source.ActorRef(64, DropNew)`

## Capabilities

### New Capabilities

_None -- this is an internal refactor with no new external behavior._

### Modified Capabilities

- `mediathek-client`: QueryAsync signature changes from `QueryAsync(QueryMediathek)` returning a message to `QueryAsync(string json, CancellationToken)` returning internal DTO. No message dependency.
- `mediathek-gateway`: Manager uses Akka.Streams internally instead of PipeTo + Stash for concurrency control and sender correlation.

## Impact

- `FunkArr.Search/MediathekClient.cs` -- simplified signature and return type
- `FunkArr.Search/MediathekViewWebManager.cs` -- rewritten with stream pipeline
- `FunkArr.Search/MediathekViewWebManagerState.cs` -- deleted (replaced by `_pending` dictionary)
- `FunkArr.Search/MediathekQueryResult.cs` -- new internal DTO
- `FunkArr.Messages/Mediathek/QueryMediathek.cs` -- unchanged (messages stay clean)
- Zero caller impact: SearchWorkers, MovieSearchWorker, and API endpoint unchanged
