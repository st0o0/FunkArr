## 1. Shared Message Type

- [x] 1.1 Add `ScoringStatsQueryResult(string RuleSetId, ScoringStatsResult Stats)` to `FunkArr.Messages/History/QueryScoringStats.cs`
- [x] 1.2 Update `HistoryWorker` to respond with `ScoringStatsQueryResult` instead of bare `ScoringStatsResult` for `QueryScoringStats`
- [x] 1.3 Update `HistoryWorkerTests` for the new response type

## 2. GitHub API Models

- [x] 2.1 Create `FunkArr.RuleSet/GitHubModels.cs` with `internal sealed record GitHubRelease` and `GitHubReleaseAsset` using `JsonPropertyName` attributes
- [x] 2.2 Rewrite `RuleSetUpdater.FindRelease()` to use `ReadFromJsonAsync<GitHubRelease[]>()` and LINQ instead of `JsonElement` traversal
- [x] 2.3 Fix `Directory.Exists` on line 114 to use `_dataFiles.Exists()`

## 3. StatsCollector Streams Backfill

- [x] 3.1 Add `IMaterializer _materializer = Context.Materializer()` field to `StatsCollector`
- [x] 3.2 Add Akka.Streams usings and `BackfillComplete` / `BackfillFailed` message types (replace individual `BackfillStatsResult`)
- [x] 3.3 Rewrite `HandleBackfillRuleSets` with `Source.From().Select().Ask<ScoringStatsQueryResult>().WithAttributes(ResumingDecider).RunWith(Sink.Seq).PipeTo`
- [x] 3.4 Add bulk `BackfillComplete` handler that applies all stats to state in one pass
- [x] 3.5 Update `StatsCollectorTests` backfill test for new response type and bulk completion

## 4. RuleSetManager Streams + IWithTimers

- [x] 4.1 Add `IWithTimers` interface and `ITimerScheduler Timers` property to `RuleSetManager`
- [x] 4.2 Add `IMaterializer _materializer = Context.Materializer()` field
- [x] 4.3 Replace `ICancelable? _flushSchedule` with `Timers.StartSingleTimer` / `Timers.IsTimerActive` in `ScheduleFlushIfNeeded`
- [x] 4.4 Remove `_flushSchedule = null` from `HandleFlush` and manual cancel from `PostStop`
- [x] 4.5 Rewrite `HandleQueryListWithStats` from `ReceiveAsync` + `Task.WhenAll` to `Receive` + Akka.Streams `.Ask()` + `PipeTo`
- [x] 4.6 Add failure message type and handler for stream failure (respond with stats-less list)
- [x] 4.7 Update `RuleSetManagerTests` for new response type and stream-based flow

## 5. Verification

- [x] 5.1 Run `dotnet build src/FunkArr.slnx`
- [x] 5.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 5.3 Run all test projects
