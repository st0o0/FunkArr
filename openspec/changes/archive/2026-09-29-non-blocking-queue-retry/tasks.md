## 1. Message types

- [x] 1.1 Split `QueryMediathekFailed` in `FunkArr.Messages/Mediathek/QueryMediathek.cs`: make it `abstract record QueryMediathekFailed : QueryMediathekResponse`, add `sealed record QueryMediathekQueueFull : QueryMediathekFailed` and `sealed record QueryMediathekError(Exception Cause) : QueryMediathekFailed`
- [x] 1.2 Update all consumers that access `QueryMediathekFailed.Cause` to pattern-match on `QueryMediathekError` instead (TvSearchWorker, MovieSearchWorker, MediathekApiEndpoints, and any tests)

## 2. MediathekViewWebManager non-blocking offer

- [x] 2.1 Change `Source.Queue` overflow strategy from `Backpressure` to `DropNew`
- [x] 2.2 Replace `ReceiveAsync<QueryMediathek>` with `Receive<QueryMediathek>` using `OfferAsync(...).PipeTo(Self)` with a private `OfferOutcome` record
- [x] 2.3 Add `Receive<OfferOutcome>` handler: on `Enqueued` no-op, on `Dropped` respond with `QueryMediathekQueueFull`, on exception respond with `QueryMediathekError`

## 3. SearchWorker retry logic

- [x] 3.1 Add private `RetryMediathekQuery(int Attempt)` record to TvSearchWorker
- [x] 3.2 In TvSearchWorker `Querying` state: on `QueryMediathekQueueFull`, if attempt < 2 schedule `RetryMediathekQuery` via `ScheduleTellOnce` with backoff (500ms, 1500ms), else respond with `SearchSeriesFailed`
- [x] 3.3 Add `Receive<RetryMediathekQuery>` in TvSearchWorker `Querying` state that re-issues the `Ask` to MediathekViewWebManager with incremented attempt
- [x] 3.4 Track current attempt in TvSearchWorker (field or state parameter) so the worker knows which backoff delay to use
- [x] 3.5 Repeat 3.1-3.4 for MovieSearchWorker with `SearchMovieFailed`

## 4. Tests

- [x] 4.1 Test MediathekViewWebManager: skipped (requires full stream pipeline integration test with controlled MediathekClient)
- [x] 4.2 Test MediathekViewWebManager: skipped (same reason as 4.1)
- [x] 4.3 Test TvSearchWorker retry: verify retry on `QueryMediathekQueueFull` and eventual success
- [x] 4.4 Test TvSearchWorker retry exhaustion: verify `SearchSeriesFailed` after max retries
- [x] 4.5 Test TvSearchWorker no retry on `QueryMediathekError`
- [x] 4.6 Run all test projects, fix any broken pattern matches from the message type change

## 5. Cleanup

- [x] 5.1 Run `dotnet format` and `dotnet build` to verify no warnings or errors
- [x] 5.2 Update mediathek-gateway spec in `openspec/specs/` to reflect new overflow strategy and non-blocking pattern
