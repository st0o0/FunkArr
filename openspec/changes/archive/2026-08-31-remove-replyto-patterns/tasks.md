## 1. MatchMagic — ExecuteScoring and MatchMagicActor

- [x] 1.1 Remove `IActorRef ReplyTo` from `ExecuteScoring` record
- [x] 1.2 Update `MatchMagicActor.Handle` to reply via `Sender.Tell` instead of `msg.ReplyTo.Tell`
- [x] 1.3 Update `MatchMagicManager.HandleScoreItems` to use `_router.Tell(msg, Sender)` instead of wrapping Sender in message

## 2. Search — MediathekViewWebManager

- [x] 2.1 Remove `IActorRef ReplyTo` from `HttpCompleted` and `HttpFailed` internal records
- [x] 2.2 Change `PipeTo(self)` to `PipeTo(self, replyTo)` in `ExecuteQuery`
- [x] 2.3 Update `HandleHttpCompleted` and `HandleHttpFailed` to use `Sender.Tell` instead of `msg.ReplyTo.Tell`

## 3. Search — TvSearchWorker and MovieSearchWorker

- [x] 3.1 Remove `IActorRef ReplyTo` from `SearchContext` in `TvSearchWorker`
- [x] 3.2 Replace all `_context.ReplyTo.Tell` with `Sender.Tell` in `TvSearchWorker`
- [x] 3.3 Remove `IActorRef ReplyTo` from `SearchContext` in `MovieSearchWorker`
- [x] 3.4 Replace all `_context.ReplyTo.Tell` with `Sender.Tell` in `MovieSearchWorker`

## 4. Tests

- [x] 4.1 Update `MatchMagicActorTests` to remove `ReplyTo` from `ExecuteScoring` construction
- [x] 4.2 Update `TvSearchWorkerTests` to verify reply goes to `Sender`
- [x] 4.3 Update `MovieSearchWorkerTests` to verify reply goes to `Sender`

## 5. Verify

- [x] 5.1 Run `dotnet build FunkArr.slnx`
- [x] 5.2 Run all affected test projects
- [x] 5.3 Run `dotnet format`
