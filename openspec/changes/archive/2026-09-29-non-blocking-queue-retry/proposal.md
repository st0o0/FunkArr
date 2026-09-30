## Why

The MediathekViewWebManager uses `ReceiveAsync` with `await OfferAsync` on a
`Source.Queue(Backpressure)`. When the queue is full, `OfferAsync` blocks the
actor's mailbox thread, preventing it from processing `StreamSuccess`/`StreamFailure`
responses that would free queue slots -- a potential deadlock. Additionally,
SearchWorkers have no retry logic: any failure from the mediathek query is
immediately terminal, even when the failure is transient (queue backpressure).

## What Changes

- **Replace `ReceiveAsync`/`await` with `Receive`/`PipeTo`** in
  MediathekViewWebManager so the actor stays responsive while queue offers are
  in flight
- **Switch `OverflowStrategy.Backpressure` to `DropNew`** for deterministic,
  immediate offer results (no lingering background tasks)
- **BREAKING: Split `QueryMediathekFailed(Exception)`** into a discriminated
  response hierarchy: `QueryMediathekQueueFull` (retryable) and
  `QueryMediathekError(Exception)` (terminal) so callers can pattern-match on
  failure reason without string/exception-type checks
- **Add retry logic in TvSearchWorker and MovieSearchWorker** for retryable
  failures (`QueueFull`) using Akka Scheduler-based backoff with bounded attempts

## Capabilities

### New Capabilities

- `mediathek-queue-offer`: Non-blocking queue offer pattern in
  MediathekViewWebManager with PipeTo and DropNew overflow strategy
- `search-worker-retry`: Retry logic in SearchWorkers for retryable mediathek
  query failures with Scheduler-based backoff

### Modified Capabilities

- `search-messages`: Split `QueryMediathekFailed` into discriminated subtypes
- `mediathek-gateway`: Change from ReceiveAsync/await to Receive/PipeTo, change
  overflow strategy from Backpressure to DropNew

## Impact

- `FunkArr.Messages/Mediathek/QueryMediathek.cs` -- response type hierarchy change
- `FunkArr.Search/MediathekViewWebManager.cs` -- queue strategy + message handling
- `FunkArr.Search/TvSearchWorker.cs` -- retry logic in Querying state
- `FunkArr.Search/MovieSearchWorker.cs` -- retry logic in Querying state
- `FunkArr.Api/MediathekApiEndpoints.cs` -- adapt pattern match for new failure types
- All consumers that pattern-match on `QueryMediathekFailed` need updating
- Test projects for Search domain
