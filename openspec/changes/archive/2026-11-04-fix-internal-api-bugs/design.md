## Context

Verified against HEAD `09f550e`:

- `EndpointExceptionFilter` catches `TimeoutException` → 504. `Akka.Actor.AskTimeoutException` inherits `AkkaException` → `Exception` (checked via reflection on Akka 1.5.73), so every Ask timeout falls into the generic branch → 500.
- Endpoint switches use `_ => ApiResults.GatewayTimeout()` for any unhandled response type, mislabelling programming errors as timeouts.
- `RingBufferSink` has one `Channel.CreateBounded<LogEntry>(100, DropOldest)`; `/api/system/logs/stream` reads it with `ReadAllAsync`. Multiple readers compete for items; with no reader the channel fills and the next client gets up to 100 stale entries.
- `POST /api/downloads/{id}/retry` does `history.Tell(new RemoveHistoryEntry(id))` and then `Ask<RetryDownloadResult>`. A rejected retry ("Item is already queued") has already lost its history entry.

This change deliberately keeps the existing files, routes and bodies; `rewrite-internal-api` restructures them afterwards.

## Goals / Non-Goals

**Goals:** fix the three bugs with minimal, contract-neutral edits, each covered by a test.

**Non-Goals:** ProblemDetails/204 contract, folder restructuring, response hierarchies for download results (all in `rewrite-internal-api`); queue push (`download-queue-push`).

## Decisions

### D1. Timeout mapping in the existing filter
Add `catch (AskTimeoutException)` next to `catch (TimeoutException)` in `EndpointExceptionFilter`, both returning `ApiResults.GatewayTimeout()` with a warning log. Change every `_ => ApiResults.GatewayTimeout()` default arm to a new `ApiResults.UnexpectedResponse(object response, ILogger)` → 500 that logs the response type.
*Alternative:* `IExceptionHandler` now. Deferred to `rewrite-internal-api`, which replaces the filter anyway.

### D2. Per-subscriber log channels
`RingBufferSink` keeps its `ConcurrentQueue` ring (500). New API: `IAsyncEnumerable<LogEntry> Subscribe(CancellationToken ct)` creates a `Channel.CreateBounded<LogEntry>(256, DropOldest, SingleReader = true)`, registers it in a `ConcurrentDictionary<Guid, Channel<LogEntry>>`, yields from it, and removes it in `finally`. `Emit` appends to the ring and `TryWrite`s into every registered channel. The `Reader` property is removed. The stream endpoint uses `sink.Subscribe(ctx.RequestAborted)`.

### D3. History cleanup owned by DownloadManager
In `DownloadManager.HandleRetry`, inside the existing `Persist(new DownloadEnqueued(...), e => { ... })` callback (which already does further work and calls `DispatchNext`, so no `DeferAsync`), add `Context.GetActor<IDownloadHistoryManager>().Tell(new RemoveHistoryEntry(cmd.DownloadId))` after the state update. The endpoint drops its history Tell. `RemoveHistoryEntry` for an id not in history is already a no-op reply, so a retry of a download without a history entry is unaffected.

## Risks / Trade-offs

- [DownloadManager now depends on the history manager at runtime] → Resolved via `Context.GetActor<IDownloadHistoryManager>()`, same pattern as other cross-singleton calls; reply from history manager is ignored (`Tell` from `Self` → dead letters avoided by sending with `ActorRefs.NoSender`).
- [Slow log client] → its own channel drops oldest; `Emit` never blocks.

## Migration Plan

Single PR; no contract or persistence changes. Rollback = revert.
