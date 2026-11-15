## 1. Messages and Persistence DTOs

- [x] 1.1 Extend HarvestFailed to `HarvestFailed(string Reason, long ListTimestamp)` in `src/FunkArr.Feed/Crawl/HarvestMessages.cs`
- [x] 1.2 Add `FeedUnchangedRun(DateTimeOffset Timestamp)` persistence event in `src/FunkArr.Persistence/Events/Feed/`
- [x] 1.3 Add Verify snapshot test for FeedUnchangedRun in `src/FunkArr.Persistence.Tests/`

## 2. FeedHarvestActor — probe error classification

- [x] 2.1 In the probe Select stage, throw a descriptive exception when the probe response is not QueryMediathekCompleted (error or QueueFull) instead of letting the Where filter produce an empty stream
- [x] 2.2 Update PipeTo failure handler to produce `HarvestFailed(ex.Message, ListTimestamp: 0)` for probe-stage exceptions (no listTimestamp available yet)
- [x] 2.3 Add test: probe returns QueryMediathekError → HarvestFailed (not HarvestUnchanged)
- [x] 2.4 Add test: probe returns QueryMediathekQueueFull → HarvestFailed
- [x] 2.5 Add test: probe returns QueryMediathekCompleted with timestamp <= cursor → HarvestUnchanged (unchanged, no regression)

## 3. FeedHarvestActor — page error handling

- [x] 3.1 Change FilterPage to throw on non-Completed page responses instead of returning ([], reachedEnd:true)
- [x] 3.2 Capture the probe's listTimestamp in the ConcatMany closure so PipeTo failure handler can include it in HarvestFailed
- [x] 3.3 Update PipeTo failure handler inside ConcatMany to produce `HarvestFailed(ex.Message, ListTimestamp: probeTimestamp)` for page-stage exceptions
- [x] 3.4 Add test: page 2 returns QueryMediathekError → HarvestFailed with probe's listTimestamp
- [x] 3.5 Add test: page returns QueryMediathekQueueFull → HarvestFailed
- [x] 3.6 Add test: cursor does NOT advance after page failure (verify via Manager state)

## 4. FeedHarvestActor — MaxPages truncation

- [x] 4.1 When the page loop reaches MaxPages without reaching end-of-window, set Truncated=true on the HarvestAccumulator
- [x] 4.2 Add test: MaxPages reached → IngestSummary.Truncated=true → Manager records CompletedWithGap
- [x] 4.3 Add test: harvest completes before MaxPages → Truncated remains false

## 5. FeedCrawlManager — gap-advance fix

- [x] 5.1 Update HarvestFailed handler to construct IngestSummary with `ListTimestamp` from the HarvestFailed message instead of hardcoded 0
- [x] 5.2 Add test: 3 consecutive HarvestFailed with valid listTimestamp → cursor advances with Gap=true
- [x] 5.3 Add test: 3 consecutive HarvestFailed with ListTimestamp=0 (probe failures) → cursor does NOT advance, FeedRunFailed persisted with failures=3
- [x] 5.4 Add test: 2 failures then 1 success → ConsecutiveFailures resets, no gap-advance

## 6. FeedCrawlManager — persist HarvestUnchanged

- [x] 6.1 Add FeedUnchangedRun recovery handler in FeedCrawlManager constructor
- [x] 6.2 Add FeedCrawlManagerState.Apply(FeedUnchangedRun) to update LastRunAt and LastOutcome
- [x] 6.3 Change HarvestUnchanged handler to Persist(FeedUnchangedRun) instead of only calling _state.WithUnchangedRun
- [x] 6.4 Include FeedUnchangedRun in snapshot (GetPersistenceState / FromPersistence) if LastRunAt is tracked there, or rely on event replay
- [x] 6.5 Add test: HarvestUnchanged → FeedUnchangedRun persisted → after recovery, LastRunAt reflects the unchanged run

## 7. Cleanup and verification

- [x] 7.1 Run `dotnet build src/FunkArr.slnx` — zero warnings
- [x] 7.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 7.3 Run `dotnet run --project src/FunkArr.Feed.Tests/FunkArr.Feed.Tests.csproj` — all tests pass
- [x] 7.4 Run `dotnet run --project src/FunkArr.Persistence.Tests/FunkArr.Persistence.Tests.csproj` — all tests pass
- [x] 7.5 Update feed-crawl-lifecycle spec to reflect HarvestFailed message change and HarvestUnchanged persistence
- [x] 7.6 Update feed-harvest-pipeline spec to reflect FilterPage and MaxPages changes
- [x] 7.7 Update feed-partial-harvest spec to confirm page failures fail the stream
