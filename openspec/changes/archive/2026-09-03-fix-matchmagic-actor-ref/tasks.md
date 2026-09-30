## 1. Remove IActorRef from message and manager

- [x] 1.1 Remove `IActorRef HistoryRef` field from `ExecuteScoring` record in `ExecuteScoring.cs`
- [x] 1.2 Remove `_historyRef` field and `IActorRef? historyRef` ctor parameter from `MatchMagicManager`, remove `_historyRef` from `ExecuteScoring` construction in `HandleScoreItems`

## 2. Add runtime resolution in MatchMagicActor

- [x] 2.1 Add `Context.GetActor<IMatchHistoryRegion>()` field to `MatchMagicActor`, send `RecordScoringResult` to the shard region directly instead of via `msg.HistoryRef`

## 3. Update tests

- [x] 3.1 Update `MatchMagicManagerTests` to remove `historyRef` probe from manager construction and adjust `ExecuteScoring` assertions
- [x] 3.2 Update `MatchMagicActorTests` to verify history recording via shard region resolution instead of `historyProbe` passed through message

## 4. Verify

- [x] 4.1 Run `dotnet build FunkArr.slnx` and `dotnet format` from `src/`
- [x] 4.2 Run `dotnet run --project FunkArr.MatchMagic.Tests/FunkArr.MatchMagic.Tests.csproj` from `src/`
