## Tasks

### Task 1: Create MediathekQueryResult DTO

Create `FunkArr.Search/MediathekQueryResult.cs` as an internal record that replaces `QueryMediathekCompleted` as the Client's return type and cache value.

```
internal sealed record MediathekQueryResult(MediathekItem[] Items, int Total);
```

**Files:** `src/FunkArr.Search/MediathekQueryResult.cs` (new)

---

### Task 2: Refactor MediathekClient to accept JSON string

Change `MediathekClient.QueryAsync` signature from `QueryAsync(QueryMediathek query, CancellationToken)` to `QueryAsync(string json, CancellationToken)`. Remove the `MediathekQueryBuilder.FromMessage` call. Return `MediathekQueryResult` instead of `QueryMediathekCompleted`. Cache `MediathekQueryResult` instead of the message. Remove `using FunkArr.Messages.Mediathek`.

**Files:** `src/FunkArr.Search/MediathekClient.cs`

---

### Task 3: Rewrite MediathekViewWebManager with Akka.Streams

Replace the entire actor implementation:
- Remove `IWithUnboundedStash`, `Stash` property
- Add private stream types: `StreamRequest`, `StreamResponse` (abstract), `StreamSuccess`, `StreamFailure`, `StreamComplete`
- Materialize stream in constructor: `Source.ActorRef<StreamRequest>(64, DropNew)` -> `Select` (QueryBuilder) -> `SelectAsyncUnordered(3)` (client.QueryAsync with try/catch) -> `Sink.ActorRef(Self, StreamComplete.Instance)`
- Add `Dictionary<Guid, IActorRef> _pending`
- `Receive<QueryMediathek>`: generate RequestId, store Sender in `_pending`, Tell `_sourceRef`
- `Receive<StreamSuccess>`: lookup + remove from `_pending`, Tell sender `QueryMediathekCompleted`
- `Receive<StreamFailure>`: lookup + remove from `_pending`, Tell sender `QueryMediathekFailed`
- `Receive<StreamComplete>`: log warning (unexpected stream termination)

**Files:** `src/FunkArr.Search/MediathekViewWebManager.cs`

---

### Task 4: Delete MediathekViewWebManagerState

Remove the state record and extension methods file. All state tracking is now via `_pending` dictionary.

**Files:** `src/FunkArr.Search/MediathekViewWebManagerState.cs` (delete)

---

### Task 5: Update tests

Update `MediathekViewWebManager` tests (if any exist) to work with the new stream-based implementation. Update `MediathekClient` tests to use the new `QueryAsync(string json, ...)` signature. Ensure `MediathekQueryBuilderTests` still pass (QueryBuilder itself is unchanged).

**Files:** `src/FunkArr.Search.Tests/` (as needed)

---

### Task 6: Build, format, run all tests

Run `dotnet build`, `dotnet format --verify-no-changes`, and all test projects to verify no regressions.
