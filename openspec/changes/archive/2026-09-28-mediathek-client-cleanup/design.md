## Context

MediathekViewWebManager is a singleton actor that gates access to the MediathekViewWeb API with a concurrency limit of 3. Currently it uses `PipeTo` with `IWithUnboundedStash` for backpressure, and the `MediathekClient` builds Akka messages directly, caching them as `QueryMediathekCompleted` records.

## Goals / Non-Goals

**Goals:**
- Separate concerns: Client does HTTP+cache, Manager does orchestration+messaging
- Replace blind `InFlight` counter with correlated request tracking
- Use Akka.Streams for the internal pipeline (first stream usage in the project)
- Messages built by the actor, not by infrastructure services

**Non-Goals:**
- Changing the external API (callers still Ask the Manager, get the same response types)
- Adding new features or changing caching behavior
- Changing the QueryBuilder logic itself

## Decisions

### 1. Akka.Streams pipeline inside the Manager actor

The Manager materializes a single long-lived stream in its constructor. The stream handles query building, HTTP execution, and throttling. The actor remains the external API.

**Why over improved PipeTo:** The stream makes the data pipeline explicit as composable stages (`Select` -> `SelectAsyncUnordered` -> `Sink`). Throttling is declarative via `SelectAsyncUnordered(3)` rather than manual slot counting.

**Why not a standalone stream actor:** The Manager is a Cluster Singleton with an established identity in the actor registry. Wrapping it in a stream actor would add indirection without benefit.

### 2. Source.ActorRef over Source.Queue

`Source.ActorRef<T>(64, OverflowStrategy.DropNew)` materializes an `IActorRef`. The actor feeds requests via `Tell` (sync, fire-and-forget).

**Why over Source.Queue:** `Source.Queue.OfferAsync` returns a `Task<IQueueOfferResult>` which is awkward in a synchronous `Receive<T>` handler. `Tell` is idiomatic Akka.

**Trade-off:** `Source.ActorRef` does not support `OverflowStrategy.Backpressure`. With `DropNew`, requests are silently dropped if the 64-element buffer fills. At realistic load (~13 concurrent callers), this never triggers. Dropped requests result in Ask timeout at the caller, which is acceptable degradation.

### 3. Sender correlation via Dictionary, not via stream

`Dictionary<Guid, IActorRef> _pending` maps RequestId to the original Sender. The stream carries only data (query + RequestId), never `IActorRef`. The actor looks up the sender when the stream result arrives via `Sink.ActorRef(Self, ...)`.

**Why:** Routing `IActorRef` through the stream couples actor concerns into the data pipeline. The dictionary also serves as the in-flight tracker: `_pending.Count` = active requests, `_pending.Keys` = active RequestIds.

### 4. Error handling: try/catch + ResumingDecider

All exceptions are caught inside `SelectAsyncUnordered` and returned as `StreamFailure` records. The stream never fails at the pipeline level. `Deciders.ResumingDecider` is applied as a safety net.

**Why not stream supervision alone:** `Directive.Resume` drops the failed element, so the caller never gets a response and times out. By catching inside the lambda, we route failures back through `Sink.ActorRef` -> `Receive<StreamFailure>` -> `sender.Tell(QueryMediathekFailed(...))`, giving the caller an explicit error.

### 5. MediathekClient accepts JSON string, returns internal DTO

`MediathekClient.QueryAsync(string json, CancellationToken)` returns `MediathekQueryResult` (internal to FunkArr.Search). The Client no longer imports `FunkArr.Messages.Mediathek`.

**Why:** The Client is a DI service, not an actor. It should not know about actor messages. The QueryBuilder transforms messages to JSON (orchestration logic) and belongs in the Manager's stream pipeline.

### 6. MediathekViewWebManagerState deleted

The `_pending` dictionary replaces all state tracking. No separate state record needed for a non-persistent actor with a single mutable field.

## Risks / Trade-offs

- **[Silent drops at overflow]** If >64 requests queue up, `DropNew` silently drops them. Mitigation: log in `Receive<QueryMediathek>` if `_pending.Count` exceeds a threshold; buffer size is 4x realistic peak load.
- **[Stream death]** If the stream terminates unexpectedly, all pending requests are orphaned. Mitigation: handle `StreamComplete` by logging a warning; pending callers hit Ask timeout. Actor supervision restarts the actor and re-materializes the stream.
- **[Status.Failure from Sink.ActorRef]** On unhandled stream failure, `Sink.ActorRef` sends `Status.Failure` to Self, violating project conventions. Mitigation: try/catch in `SelectAsyncUnordered` + `ResumingDecider` ensures the stream never fails.
