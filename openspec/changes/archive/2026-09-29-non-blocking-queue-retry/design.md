## Context

MediathekViewWebManager is a singleton actor that gates all MediathekViewWeb API
access through an Akka.Streams pipeline (`Source.Queue` -> `SelectAsyncUnordered`
-> `Sink.ActorRef`). Currently it uses `ReceiveAsync` with `await OfferAsync` on a
`Source.Queue(64, Backpressure)`. This blocks the actor's mailbox thread when the
queue is full, preventing it from processing stream responses that would free
slots -- a potential deadlock.

SearchWorkers (TvSearchWorker, MovieSearchWorker) use a single `Ask` with no
retry. Any mediathek query failure is immediately terminal.

`QueryMediathekFailed(Exception Cause)` is a flat record that forces callers to
inspect exception types or messages to determine if a failure is retryable.

## Goals / Non-Goals

**Goals:**

- Eliminate the deadlock risk in MediathekViewWebManager by making queue offers
  non-blocking
- Give SearchWorkers the ability to retry transient (queue-full) failures with
  bounded backoff
- Provide type-safe discriminated failure responses so callers can pattern-match
  on failure reason

**Non-Goals:**

- HTTP-level retry (Polly/resilience on MediathekClient) -- separate concern
- Changing the stream pipeline internals (SelectAsyncUnordered, Sink.ActorRef)
- Circuit breaker or rate limiting
- Changing the 15s Ask timeout in workers or 30s overall timeout in SearchManager

## Decisions

### 1. OverflowStrategy: DropNew instead of Backpressure

**Decision:** Use `OverflowStrategy.DropNew` with `Source.Queue`.

**Why:** With `PipeTo` (non-blocking), `OfferAsync` on a `DropNew` queue returns
immediately with either `Enqueued` or `Dropped`. This gives deterministic,
synchronous-style control flow. Backpressure would leave `OfferAsync` Tasks
hanging in the background, creating ghost-offer problems on timeout/retry.

**Alternatives considered:**
- `Backpressure` + timeout via Scheduler: Works but the old `OfferAsync` Task
  continues running after timeout. If it eventually succeeds, the element enters
  the queue after the caller already received a failure -- ghost offer, potential
  double processing. Requires generation IDs to ignore stale results.
- `DropHead`/`DropTail`: Silently drops an already-enqueued element whose sender
  is tracked in `_pending` -- that sender never gets a response (orphaned).

### 2. Non-blocking offer: Receive + PipeTo instead of ReceiveAsync + await

**Decision:** Replace `ReceiveAsync<QueryMediathek>(async ...)` with
`Receive<QueryMediathek>(...)` using `OfferAsync(...).PipeTo(Self)`.

**Why:** `ReceiveAsync` blocks the actor mailbox until the async handler completes.
With `Backpressure`, the handler awaits a Task that needs stream responses to
free space, but those responses are queued in the same mailbox -- deadlock. With
`PipeTo`, the `OfferAsync` Task runs in the background, the actor continues
processing other messages, and the result arrives as a regular message.

### 3. Discriminated failure responses instead of Exception-only

**Decision:** Split `QueryMediathekFailed(Exception Cause)` into:
- `abstract record QueryMediathekFailed : QueryMediathekResponse`
- `sealed record QueryMediathekQueueFull : QueryMediathekFailed`
- `sealed record QueryMediathekError(Exception Cause) : QueryMediathekFailed`

**Why:** Callers need to distinguish retryable (queue full) from terminal (HTTP
error, parse error) failures via pattern matching, not exception type inspection
or string matching. The abstract base preserves backward-compatible catch-all
matching (`case QueryMediathekFailed`).

**Alternatives considered:**
- Custom exception hierarchy (`QueueFullException`, etc.): Using exceptions for
  control flow is an antipattern. Queue-full is normal backpressure signaling.
- Enum reason field on the existing record: Less expressive, can't carry
  type-specific data (e.g., `Exception` only on errors, `RetryAfter` on rate
  limits in the future).

### 4. Retry in SearchWorkers, not in MediathekViewWebManager

**Decision:** Workers own retry logic. The Manager is a dumb pipe.

**Why:**
- Manager stays simple: receive query, offer to queue, respond with result or
  failure. No retry state, no timers, no attempt tracking.
- Workers already have Become-based state machines. Retry fits naturally as a
  re-entry into the `Querying` state.
- Workers know their context (how important the search is, how long they've been
  running, the overall Ask timeout budget).
- Avoids retry storms: each worker has independent backoff. Under load, retries
  are spread across time naturally.
- Clean lifecycle: a retry is a new Ask, a fresh request to the Manager. No
  shared state between Manager and Worker about retry attempts.

### 5. Scheduler-based backoff with bounded attempts

**Decision:** Workers use `Context.System.Scheduler.ScheduleTellOnce` to delay
retries. Maximum 2 retry attempts (3 total tries). Backoff: 500ms, 1500ms.

**Why:** Akka Scheduler is the idiomatic way to delay messages to Self. The
delays are short enough to fit within the 15s Ask timeout budget (worst case:
0 + 500 + 1500 = 2s of delays + 3x query time). The retry message is a private
record handled in the `Querying` state.

### 6. Internal QueueOfferResult message in MediathekViewWebManager

**Decision:** Add a private `QueueOfferResult` record that wraps the
`IQueueOfferResult` from `OfferAsync.PipeTo`. Handle `Enqueued` (no-op, wait for
stream) and everything else (respond with `QueryMediathekQueueFull`).

**Why:** PipeTo needs a typed message. The `success`/`failure` lambdas in PipeTo
map the raw Akka result into the actor's internal message protocol.

## Risks / Trade-offs

**[DropNew drops silently under sustained load]** If the queue is full for
extended periods, many requests get `QueueFull` responses and workers retry,
adding more pressure. -> Mitigation: Bounded retries (max 2) with backoff.
Workers that exhaust retries fail fast. The 64-slot queue at 3 concurrent with
~500ms per query drains at ~6/sec, recovering within seconds of a burst.

**[Breaking change on QueryMediathekFailed]** All code that pattern-matches on
`QueryMediathekFailed` or accesses `.Cause` needs updating. -> Mitigation:
Version 0.x, breaking changes are fine. The `abstract` base means existing
`case QueryMediathekFailed` catch-alls still compile. Only `.Cause` access needs
updating to match on `QueryMediathekError` specifically.

**[Orphaned _pending entries on DropNew]** When `OfferAsync` returns `Dropped`,
the request never enters the stream, so no `StreamSuccess`/`StreamFailure` will
arrive. -> Mitigation: The Manager immediately responds with `QueryMediathekQueueFull`
and removes the `_pending` entry in the `QueueOfferResult` handler.

**[PipeTo failure path]** If `OfferAsync` itself throws (stream terminated, etc.),
the PipeTo `failure` lambda fires. -> Mitigation: Map to `QueueOfferResult` with
the exception, handle the same as Dropped (respond with `QueryMediathekError`).
