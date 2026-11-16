# FunkArr Comprehensive QA Test Plan

> **Companion to E2E-TEST-PLAN.md** — covers gaps in persistence, enrichment,
> degraded mode, cross-domain integration, security, data integrity, and
> performance testing discovered during a systematic code-review exploration.
>
> **This document does NOT replace E2E-TEST-PLAN.md.** The E2E plan covers
> happy-path browser + API acceptance testing. This plan covers everything else:
> bug verification, recovery, edge cases, and stress testing.

**Finding references**: Tests marked with `[Finding #N]` trace back to concrete
bugs or risks discovered during the code-review exploration. These are not
hypothetical — they are verified by reading the source code.

**Test types**: Each test is tagged with its recommended execution method:

| Tag | Meaning |
|-----|---------|
| `[unit]` | xUnit test against isolated class/actor |
| `[actor]` | Akka TestKit test with actor restart/recovery |
| `[integration]` | In-process HTTP test via FunkArrFixture |
| `[e2e]` | Docker Compose end-to-end (manual or Playwright) |
| `[api]` | Direct API call (curl/PowerShell) |
| `[manual]` | Requires manual observation or timing |

---

## Part A: Bug Verification Tests

Tests that target concrete bugs and risks discovered during code review.
Priority: fix the bug, then write the test as a regression guard.

### A.1 Feed Crawl Data Loss

**Context**: `FeedCrawlManager.cs`, `FeedHarvestActor.cs` in `src/FunkArr.Feed/Crawl/`.
The feed ingest pipeline has several paths where data is silently lost.

- [ ] **A.1.1** `[actor]` Page error advances cursor — silent data loss `[Finding #2]`
  Simulate a MediathekViewWeb 503 on page 2 of a multi-page crawl. Verify that
  the cursor does NOT advance past the failed page. Currently `FilterPage` returns
  `([], reachedEnd: true)` for any non-Completed response, which advances the cursor
  and loses all items on remaining pages.
  **File**: `FeedHarvestActor.cs` lines 187-192.

- [ ] **A.1.2** `[actor]` MaxPages cap without Truncated flag `[Finding #3]`
  Configure a crawl window that requires more pages than `MaxPages`. Verify that
  either a `CompletedWithGap` outcome is produced or the cursor does not advance
  past the last successfully fetched page. Currently the loop stops without
  setting `Truncated`, so the cursor advances silently.
  **File**: `FeedHarvestActor.cs` line 112.

- [ ] **A.1.3** `[unit]` Unreachable gap-advance branch `[Finding #1]`
  The "3 consecutive failures then advance cursor with Gap" branch in
  `FeedCrawlManager.CompleteHarvest` is unreachable. `HarvestFailed` sets
  `ListTimestamp = 0`, but the condition checks `summary.ListTimestamp > _state.Cursor`.
  Verify this with a test that triggers 3+ consecutive failures and asserts
  the cursor never advances. Then fix the branch.
  **File**: `FeedCrawlManager.cs` lines 103, 161-176.

- [ ] **A.1.4** `[actor]` MVW outage at probe reports Unchanged, not Failed `[Finding #4]`
  Simulate a MediathekViewWeb timeout or error during the initial probe (first
  query that checks the newest timestamp). Verify that the outcome is `Failed`
  and `ConsecutiveFailures` increments. Currently a probe failure produces
  `HarvestUnchanged`, which does not increment the failure counter.
  **File**: `FeedHarvestActor.cs` probe logic; `FeedCrawlManager.cs` HandleUnchanged.

- [ ] **A.1.5** `[actor]` HarvestUnchanged state lost on restart `[Finding #12]`
  Trigger a harvest that returns Unchanged. Verify `LastRunAt` and `LastOutcome`
  are set. Restart the actor. Verify they are restored. Currently these fields
  are set only in-memory via `WithUnchangedRun()` and not persisted.
  **File**: `FeedCrawlManagerState.cs`.

- [ ] **A.1.6** `[actor]` Queue saturation during crawl drops items silently `[Finding #6 (partial)]`
  Saturate the MediathekViewWeb queue (64 slots) while a feed crawl is running.
  Verify that QueueFull responses during a crawl either retry or mark the run
  as failed/partial. Currently `FeedHarvestActor.FilterPage` treats QueueFull
  as end-of-data.
  **File**: `FeedHarvestActor.cs` FilterPage; `MediathekViewWebManager.cs` queue.

### A.2 Enrichment Cache and Error Handling

**Context**: `TvdbClient.cs`, `TmdbClient.cs`, `TvdbEnrichmentActor.cs`,
`TmdbEnrichmentActor.cs` in `src/FunkArr.Enrichment/`.

- [ ] **A.2.1** `[unit]` TVDB partial result cached for 7 days `[Finding #5]`
  Mock `TvdbClient.FetchEpisodesAsync` to return a partial episode list (e.g.,
  first page succeeds, second page returns HTTP 500). Verify the partial result
  is NOT cached. Currently `GetEpisodesAsync` caches whatever `FetchEpisodesAsync`
  returns, including partial/empty lists from failed pages, for 7 days.
  **File**: `TvdbClient.cs` cache.Set in GetEpisodesAsync.

- [ ] **A.2.2** `[unit]` TMDB HTTP error misclassified as MovieNotFound `[Finding #8]`
  Mock TMDB to return HTTP 429 (rate limited) or 503 (server error). Verify the
  error is distinguishable from "movie not found". Currently `FetchAsync` swallows
  all non-success status codes to `null`, which becomes `MovieNotFoundException`.
  **File**: `TmdbClient.cs` FetchAsync; `TmdbEnrichmentActor.cs`.

- [ ] **A.2.3** `[unit]` TMDB API key leaked in error logs `[Finding #6]`
  Mock TMDB to return an error. Capture log output. Verify the `api_key` query
  parameter is NOT present in any log line. Currently `TmdbClient.FetchAsync`
  logs `{Url}` on failure, and the URL contains `api_key=<value>`.
  **File**: `TmdbClient.cs` FetchAsync logging.

- [ ] **A.2.4** `[actor]` Ask timeout mismatch — late enrichment replies `[Finding #7]`
  Configure enrichment to take 12 seconds (between the 10s Ask timeout and 30s
  resilience timeout). Verify the evaluation worker handles the timeout gracefully
  and the late reply does not cause errors. Currently the worker times out at 10s,
  but the enrichment actor keeps working for up to 30s, and the late reply goes
  to dead letters.
  **File**: `TvEvaluationWorker.cs` _enrichmentTimeout; `EnrichmentSetupContainer.cs`.

- [ ] **A.2.5** `[unit]` TMDB alt-title failure cached as empty for 7 days
  Mock TMDB alt-titles endpoint to return 500. Verify empty alt-title array is
  NOT cached. Currently the result is cached regardless of success.
  **File**: `TmdbClient.cs` GetMovieDataAsync/cache logic.

### A.3 Download Lifecycle Bugs

**Context**: `DownloadWorker.cs`, `DownloadManager.cs`, `DataPaths.cs`,
`SubtitleDownloader.cs` in `src/FunkArr.Download/` and `src/FunkArr.Core/Data/`.

- [ ] **A.3.1** `[unit]` Path traversal via unsanitized titles `[Finding #19]`
  Create a download with title containing `../../../etc/passwd` or `foo\bar` or
  `foo/bar`. Verify that `DataPaths` resolves the path within the download
  directory and does not escape it. Currently titles from MediathekViewWeb are
  used raw in file and directory names with no sanitization.
  **File**: `DataPaths.cs` path construction.

- [ ] **A.3.2** `[actor]` Slot leak on retry-wait recovery `[Finding #20]`
  1. Start a download, trigger a transient failure so the worker enters retry-wait
     (Status=Failed, retry timer armed).
  2. Kill and restart the worker actor.
  3. Verify the manager detects the stuck slot and either frees it or re-initializes
     the worker. Currently: recovered worker stays Failed with no timer, manager
     re-dispatches with StartDownload, HandleStart returns early because
     Status != Initialized, no SlotFree is sent, slot is permanently occupied.
  **File**: `DownloadWorker.cs` OnRecoveryCompleted, HandleStart;
  `DownloadManager.cs` ResetDispatched.

- [ ] **A.3.3** `[unit]` Retry off-by-one `[Finding #21]`
  Set `MaxRetries = 3`. Trigger transient failures. Count total attempts.
  Verify whether 3 means "3 retries after initial attempt" (4 total) or
  "3 total attempts" (2 retries). Currently `ShouldRetry` checks
  `Attempt < MaxRetries`, so MaxRetries=3 gives 3 total attempts (2 retries).
  Document the intended semantics and fix or test accordingly.
  **File**: `DownloadWorker.cs` ShouldRetry; `DownloadOptions.cs` MaxRetries.

- [ ] **A.3.4** `[unit]` Retry loses download category `[Finding #22]`
  Retry a failed movie download. Verify the re-queued item retains its category
  (Movie). Currently `DownloadManager.HandleRetry` persists `DownloadEnqueued`
  without category, and `Apply` defaults null to `Show`.
  **File**: `DownloadManager.cs` line 202; `DownloadManagerState.cs` Apply.

- [ ] **A.3.5** `[unit]` HasSubtitles flag incorrect after subtitle failure `[Finding #24]`
  Trigger a download where the subtitle URL is set but the subtitle download
  fails (HTTP 404). Verify that `WorkerStatusResult.HasSubtitles` is `false`.
  Currently it checks `Media.SubtitleUrl != null` regardless of download outcome.
  **File**: `DownloadWorkerState.cs` or status mapping.

- [ ] **A.3.6** `[actor]` ScheduleEnabled race on recovery `[Finding #25]`
  Configure a download schedule that is currently outside the allowed window.
  Queue a download, then restart the container. Verify the download does NOT
  dispatch before the scheduler sends `ScheduleDisabled`. Currently
  `ScheduleEnabled` defaults to `true` and is not persisted, so
  `DownloadManager.RecoveryCompleted` calls `DispatchNext()` immediately.
  **File**: `DownloadManager.cs` RecoveryCompleted; `DownloadManagerState.cs`.

- [ ] **A.3.7** `[unit]` Delete creates Failed history entry `[Finding #26]`
  Delete an active download via `DELETE /api/downloads/queue/{id}`. Verify the
  history entry has a status that distinguishes cancellation from failure.
  Currently the cancelled download goes through `FfmpegResult("Cancelled", Permanent)`,
  which records a `Failed` history entry.
  **File**: `DownloadWorker.cs` HandleCancel → FfmpegResult path.

- [ ] **A.3.8** `[unit]` Subtitle cancellation classified as Permanent `[Finding #27]`
  Cancel a download while the subtitle is being fetched. Verify the failure is
  classified as cancellation, not a permanent failure. Currently
  `OperationCanceledException` during subtitle fetch rethrows and becomes
  `FfmpegResult(Permanent)`, preventing any retry.
  **File**: `SubtitleDownloader.cs`; `Remuxer.cs` catch path.

### A.4 Search Edge Cases

**Context**: `SearchManager.cs` in `src/FunkArr.Search/Manager/`,
`MediathekViewWebManager.cs`, `VariantDetector.cs` in `src/FunkArr.Search/`.

- [ ] **A.4.1** `[actor]` Both-search partial timeout is silent success `[Finding #10]`
  Trigger a search for `MediaType.Both`. Make the TV shard respond and the movie
  shard time out. Verify the result is either marked as partial or a warning is
  logged. Currently the partial result is returned as a normal success with no
  indication that movie results are missing.
  **File**: `SearchManager.cs` timeout handler for Both searches.

- [ ] **A.4.2** `[actor]` MediathekViewWebManager StreamComplete crash `[Finding #15]`
  Trigger `StreamComplete` on the MVW manager's internal stream. Verify the actor
  either handles it gracefully or restarts cleanly. Currently it throws
  `InvalidOperationException`, crashing the actor. The `Pending` dictionary is
  lost, and in-flight callers never receive a reply.
  **File**: `MediathekViewWebManager.cs` StreamComplete handler.

- [ ] **A.4.3** `[unit]` VariantDetector NFD Unicode mismatch `[Finding #13]`
  Pass a title containing decomposed Unicode (e.g., `a` + combining diaeresis
  instead of `ä`) to `VariantDetector.Detect`. Verify the correct variant is
  detected. Currently decomposed forms do not match the precomposed ä/ö/ü
  literals in the regex patterns.
  **File**: `VariantDetector.cs`.

- [ ] **A.4.4** `[unit]` VariantDetector false positives on OV/DGS `[Finding #14]`
  Pass titles containing "OV" or "DGS" as part of unrelated words or abbreviations.
  Verify they are NOT classified as OriginalVersion or SignLanguage. The `\bOV\b`
  and `\bDGS\b` patterns can match in unexpected contexts.
  **File**: `VariantDetector.cs`.

- [ ] **A.4.5** `[unit]` VariantDetector regex timeout fallthrough `[Finding #13]`
  Pass a title that causes regex backtracking close to the 100ms timeout. Verify
  the behavior is predictable. Currently `RegexMatchTimeoutException` is caught
  and treated as no match (Standard), which is silent.
  **File**: `VariantDetector.cs`.

### A.5 Cache Invalidation

- [ ] **A.5.1** `[integration]` Newznab feed cache stale after new feed entries `[Finding #9]`
  1. Request `GET /index/api?t=tvsearch&apikey=<key>` (empty query, cached).
  2. Publish new feed entries via `POST /api/feed/crawl`.
  3. Request the same Newznab URL again.
  4. Verify the response includes the new entries.
  Currently the Newznab "feed" tag is never evicted — only the "rulesets" tag
  is evicted on ruleset changes. New feed entries are stale for up to 5 minutes.
  **File**: `NewznabFeedCache.cs`; `RuleSetApiEndpoints.cs` (only evicts "rulesets").

- [ ] **A.5.2** `[unit]` TVDB cache not invalidated after successful re-fetch
  After a cache entry for a series is populated, trigger a new fetch for the same
  series (e.g., after cache expiry). Verify the new result replaces the old one.
  This is a baseline test — currently cache expiry at 7 days handles this, but
  verify it works correctly.
  **File**: `TvdbClient.cs` cache logic.

---

## Part B: Persistence and Recovery

No actor restart or recovery test exists anywhere in the test suite today.
These tests verify that persistent actors survive restarts and recover state
correctly.

### B.1 DownloadManager Recovery

**Actor**: `DownloadManager` (Singleton, PersistenceId "download-manager").
**File**: `src/FunkArr.Download/Manager/DownloadManager.cs`.
**Snapshot interval**: 25 events.

- [ ] **B.1.1** `[actor]` Snapshot restore produces correct state
  Persist >25 events (enqueue, dispatch, dequeue, etc.), triggering a snapshot.
  Restart the actor. Verify state matches pre-restart: queue contents, dispatch
  set, pause state, priority ordering.

- [ ] **B.1.2** `[actor]` ResetDispatched on recovery
  Dispatch 2 downloads (move to Dispatched state). Restart the actor. Verify
  both items are back in Queued (not Dispatched) and `DispatchNext()` re-dispatches
  them.

- [ ] **B.1.3** `[actor]` Priority order preserved after recovery
  Enqueue items with different priorities (Low, Normal, High). Restart. Verify
  queue order is maintained (High first, then Normal, then Low).

- [ ] **B.1.4** `[actor]` Pause state survives restart
  Pause the pipeline. Restart the actor. Verify the pipeline remains paused and
  `DispatchNext` does not dispatch items.

- [ ] **B.1.5** `[actor]` Snapshot interval triggers at exactly 25 events
  Persist exactly 25 events. Verify `SaveSnapshot` is called. Persist 24 events
  in a fresh actor. Verify no snapshot.

- [ ] **B.1.6** `[actor]` DeleteMessages/DeleteSnapshots on snapshot success
  Trigger a snapshot. Verify `DeleteMessages(sequenceNr)` and
  `DeleteSnapshots(sequenceNr - 1)` are called on `SaveSnapshotSuccess`.
  **File**: `DownloadManager.cs` lines 70-83.

### B.2 DownloadWorker Recovery

**Actor**: `DownloadWorker` (Shard Region, PersistenceId "download-{entityId}").
**File**: `src/FunkArr.Download/Worker/DownloadWorker.cs`.
**Snapshot interval**: NONE (no snapshots, no DeleteMessages).

- [ ] **B.2.1** `[actor]` Full journal replay without snapshot
  Initialize a worker, start a download, complete it. Restart. Verify the full
  event journal is replayed and the state matches.

- [ ] **B.2.2** `[actor]` Transient phase reset to Initialized
  Start a download (phase = VideoDownload, which is transient). Restart. Verify
  `ResetAfterRecovery()` sets Status=Initialized, Phase=Initialized. The worker
  should be ready to receive a new `StartDownload`.

- [ ] **B.2.3** `[actor]` Failed phase recovery — no timer, stuck state
  Put the worker in Failed status (transient failure, retry timer armed). Restart.
  Verify the behavior: currently the worker stays Failed with no retry timer,
  and the manager's `StartDownload` is ignored. This is `[Finding #20]`.

- [ ] **B.2.4** `[actor]` Completed phase replay
  Complete a download. Restart. Verify state shows Completed with correct metadata.

- [ ] **B.2.5** `[actor]` Journal growth without cleanup
  Persist 100 events for a single worker. Verify journal size. Document the growth
  rate. Currently there are no snapshots and no `DeleteMessages`, so the journal
  grows unbounded. `[Finding #18]`

### B.3 DownloadHistoryManager Recovery

**Actor**: `DownloadHistoryManager` (Singleton, PersistenceId "download-history").
**File**: `src/FunkArr.Download/History/DownloadHistoryManager.cs`.
**Snapshot interval**: 25 events.

- [ ] **B.3.1** `[actor]` Snapshot + event replay produces complete history
  Record 30 history entries (triggering a snapshot at 25). Restart. Verify all
  30 entries are present and queryable.

- [ ] **B.3.2** `[actor]` Trimmed entries stay trimmed after recovery
  Record entries, trim some, restart. Verify trimmed entries do not reappear.

### B.4 FeedCrawlManager Recovery

**Actor**: `FeedCrawlManager` (Singleton, PersistenceId "feed-crawl").
**File**: `src/FunkArr.Feed/Crawl/FeedCrawlManager.cs`.
**Snapshot interval**: 20 events.

- [ ] **B.4.1** `[actor]` Cursor survives restart
  Complete a crawl (advances cursor). Restart. Verify the cursor value is restored
  from the journal/snapshot.

- [ ] **B.4.2** `[actor]` ConsecutiveFailures survives restart
  Trigger 2 failed crawls. Restart. Verify `ConsecutiveFailures == 2`.

- [ ] **B.4.3** `[actor]` Harvester child recreated on recovery
  Restart the actor. Verify the "harvester" child actor is created during
  `RecoveryCompleted`.

- [ ] **B.4.4** `[actor]` First crawl fires after 30-second startup delay
  Restart with Enabled=true. Verify no crawl starts immediately, but one starts
  after approximately 30 seconds.

### B.5 FeedStoreManager Recovery

**Actor**: `FeedStoreManager` (Singleton, PersistenceId "feed-store").
**File**: `src/FunkArr.Feed/Store/FeedStoreManager.cs`.
**Snapshot interval**: 50 events.

- [ ] **B.5.1** `[actor]` All entries restored from snapshot + events
  Publish 60 entries (triggering a snapshot at 50). Restart. Verify all 60
  entries are present and queryable with correct ordering.

- [ ] **B.5.2** `[actor]` Trim timer re-armed on recovery
  Restart. Verify the 1-hour trim timer is scheduled during `RecoveryCompleted`.

- [ ] **B.5.3** `[actor]` 50-event snapshot interval
  Publish exactly 50 entries. Verify snapshot. Publish 49. Verify no snapshot.

### B.6 HistoryWorker Recovery

**Actor**: `HistoryWorker` (Shard Region, PersistenceId "history-{nttId}").
**File**: `src/FunkArr.History/HistoryWorker.cs`.
**Snapshot interval**: Configurable via `ScoringHistoryOptions.SnapshotInterval` (default 20).

- [ ] **B.6.1** `[actor]` OnReplaySuccess trims old snapshots
  Record 30 history snapshots with `MaxSnapshots = 20`. Restart. Verify
  `OnReplaySuccess` trims to 20 entries. This is the ONLY actor that trims on
  replay.

- [ ] **B.6.2** `[actor]` Configurable SnapshotInterval via IOptionsMonitor
  Set `SnapshotInterval = 5`. Record 5 events. Verify snapshot. Change to 10.
  Record 5 more. Verify no snapshot at event 10 (interval is now 10).

- [ ] **B.6.3** `[actor]` Stats correct after replay
  Record several scoring entries. Restart. Query stats. Verify they match
  pre-restart values.

### B.7 Container Restart (E2E)

**Prerequisite**: Running `docker compose -f docker-compose.dev.yml` with data populated
from E2E-TEST-PLAN.md execution.

- [ ] **B.7.1** `[e2e]` `docker restart funkarr` — all state intact
  Restart the FunkArr container. Wait for health check to pass. Verify:
  - Dashboard stats unchanged
  - Download history entries unchanged
  - Scoring history entries unchanged
  - Rulesets unchanged (community + local overlays)
  - Feed entries unchanged

- [ ] **B.7.2** `[e2e]` Active downloads re-queue and restart
  Start a download, then `docker restart funkarr` while it is in progress.
  Verify the download re-queues and eventually completes.

- [ ] **B.7.3** `[e2e]` Feed cursor resumes from last position
  Complete a crawl. `docker restart funkarr`. Trigger another crawl. Verify
  the cursor starts from where it left off (not from zero).

- [ ] **B.7.4** `[e2e]` Download history preserved
  Verify `GET /api/downloads/history` returns the same entries before and after
  restart. Compare counts and a sample entry's ID.

- [ ] **B.7.5** `[e2e]` Scoring history preserved
  Verify `GET /api/rulesets/{id}/history` returns the same entries before and
  after restart.

- [ ] **B.7.6** `[e2e]` Rulesets unchanged
  Verify `GET /api/rulesets` returns the same list. Verify a merged ruleset
  still shows its local overlay.

- [ ] **B.7.7** `[e2e]` SQLite DB integrity after unclean shutdown
  `docker kill funkarr` (SIGKILL, no graceful shutdown). Start again. Verify
  no data corruption and all state is accessible. SQLite WAL should handle this.

- [ ] **B.7.8** `[e2e]` Postgres backend — same recovery tests
  Run with `docker-compose.postgres.yml`. Repeat B.7.1 through B.7.7 against
  the Postgres persistence backend.

---

## Part C: Enrichment

The enrichment domain has no end-to-end tests today. Unit tests cover matching
logic but not HTTP calls, caching, actor behavior, or integration with scoring.

### C.1 TVDB Flow

**Actors**: `EnrichmentManager`, `TvdbEnrichmentActor`.
**Client**: `TvdbClient` (TVDB v4 API, JWT auth).
**Files**: `src/FunkArr.Enrichment/Tvdb/`.

- [ ] **C.1.1** `[unit]` Successful episode lookup
  Mock TVDB to return a full episode list for a known series. Call
  `TvdbClient.GetEpisodesAsync`. Verify episodes are returned and cached.

- [ ] **C.1.2** `[unit]` Pagination — series with >1 page of episodes
  Mock TVDB to return 2 pages (first page has `links.next` set). Verify both
  pages are fetched and combined.

- [ ] **C.1.3** `[unit]` 401 re-authentication succeeds
  Mock TVDB to return 401 on first request, then 200 after re-login. Verify
  the re-auth flow works and episodes are returned.

- [ ] **C.1.4** `[unit]` 401 re-authentication fails
  Mock TVDB to return 401 on all requests. Verify the client returns an empty
  list (not an exception) and the empty list is NOT cached.

- [ ] **C.1.5** `[actor]` Season filter — only matching season returned
  Send `EnrichEpisodes` with `Season = 2024`. Mock TVDB with episodes from
  seasons 2022-2026. Verify only season 2024 episodes are passed to the enricher.

- [ ] **C.1.6** `[actor]` Season fallback applies 0.9 confidence penalty
  Send `EnrichEpisodes` with `Season = 2024` and candidates that match episodes
  from other seasons. Verify the fallback runs against all episodes and applies
  the 0.9 confidence multiplier. Currently untested — the penalty logic lives
  only in `TvdbEnrichmentActor`, not in `EpisodeEnricher`.
  **File**: `TvdbEnrichmentActor.cs`.

- [ ] **C.1.7** `[unit]` Cache hit skips HTTP call
  Populate the cache for a series. Call `GetEpisodesAsync` again. Verify no
  HTTP request is made.

- [ ] **C.1.8** `[unit]` Cache expires after 7 days
  Populate cache, advance time by 7+ days (via `TimeProvider`). Verify a new
  HTTP call is made.

### C.2 TMDB Flow

**Actors**: `EnrichmentManager`, `TmdbEnrichmentActor`.
**Client**: `TmdbClient` (TMDB v3 API, query-param auth).
**Files**: `src/FunkArr.Enrichment/Tmdb/`.

- [ ] **C.2.1** `[unit]` Lookup by TmdbId
  Mock TMDB `GET movie/{id}` + alt titles. Verify movie data is returned and cached.

- [ ] **C.2.2** `[unit]` Lookup by ImdbId (find endpoint)
  Mock TMDB `GET find/{imdbId}`. Verify the movie is found and fetched.

- [ ] **C.2.3** `[actor]` TmdbId takes precedence over ImdbId
  Send `EnrichMovies` with both TmdbId and ImdbId set. Verify only the TmdbId
  path is used (no `find` call).

- [ ] **C.2.4** `[actor]` Neither ID set
  Send `EnrichMovies` with both IDs null. Verify `EnrichMoviesFailed` is returned
  with a clear error message.

- [ ] **C.2.5** `[unit]` Alternative title matching
  Mock TMDB with a movie whose original title differs from alt titles. Verify
  the enricher considers all titles for matching.

- [ ] **C.2.6** `[unit]` HTTP 429 distinguishable from MovieNotFound `[Finding #8]`
  (See A.2.2 — this is the fix verification test.)

- [ ] **C.2.7** `[unit]` Cache hit skips HTTP call
  Same pattern as C.1.7 for TMDB.

### C.3 Enrichment Config

**Files**: `EnrichmentConfig` in `src/FunkArr.Messages/Enrichment/`,
`DiskToMatchingExtensions.cs` in `src/FunkArr.RuleSet/DiskModel/`.

- [ ] **C.3.1** `[actor]` Disabled config — no external API calls
  Send enrichment with `Enabled = false`. Verify no TVDB/TMDB HTTP calls.

- [ ] **C.3.2** `[unit]` Title-only method
  Set `Methods = [Title]`. Verify only title similarity is used for matching.

- [ ] **C.3.3** `[unit]` Airdate-only method
  Set `Methods = [Airdate]`. Verify only airdate matching is used.

- [ ] **C.3.4** `[unit]` Title + Airdate combined
  Default config. Verify title is tried first, airdate as fallback.

- [ ] **C.3.5** `[unit]` Runtime mode — Tiebreaker vs Filter
  With `Runtime.Mode = Filter`: verify episodes outside runtime tolerance are
  dropped. With `Tiebreaker`: verify runtime is used to break ties but does
  not exclude candidates.

- [ ] **C.3.6** `[unit]` Custom threshold values
  Set `Title.Threshold = 0.5` and verify more fuzzy matches accepted. Set to
  `0.95` and verify only near-exact matches pass.

- [ ] **C.3.7** `[unit]` Null config uses defaults
  Send enrichment with `Config = null`. Verify defaults apply (Enabled=true,
  Methods=[Title, Airdate], Threshold=0.7).

- [ ] **C.3.8** `[actor]` No TVDB ID — clear error message
  Send `EnrichEpisodes` with `TvdbId = 0`. Verify `EnrichEpisodesFailed` with
  a message indicating the ID is missing.

### C.4 Integration with Scoring

- [ ] **C.4.1** `[e2e]` Enrichment success visible in scoring trace
  Trigger a Sonarr search for a series with enrichment enabled. Check scoring
  detail (`GET /api/rulesets/{id}/history/{requestId}`). Verify `enrichmentTrace`
  entries have `confidence > 0`, `method`, `resolvedSeason`, `resolvedEpisode`.

- [ ] **C.4.2** `[actor]` Enrichment failure — scoring continues
  Mock TVDB to fail. Trigger a search. Verify scoring completes without
  enrichment data and the result is returned to the caller.

- [ ] **C.4.3** `[actor]` Enrichment timeout — scoring continues
  Mock TVDB to respond after 15 seconds (past the 10s Ask timeout). Verify
  scoring completes without enrichment.

- [ ] **C.4.4** `[api]` Test scoring with enrichment
  `POST /api/rulesets/test` with candidates and enrichment config. Verify the
  response includes enrichment data in traces.

---

## Part D: Degraded Mode

What happens when external services are unavailable?

### D.1 MediathekViewWeb Down

**Context**: `MediathekViewWebManager.cs`, `MediathekClient.cs`.
Resilience: circuit breaker at 20% failure ratio, 60s break. Total timeout 45s,
attempt timeout 15s.

- [ ] **D.1.1** `[actor]` HTTP 503 — circuit breaker opens
  Send 5+ search requests while MVW returns 503. Verify the circuit breaker
  opens after hitting the 20% threshold. Verify subsequent requests fail fast
  without making HTTP calls. Verify the breaker closes after 60s.

- [ ] **D.1.2** `[actor]` Slow response — timeout propagation
  Mock MVW to respond after 50 seconds. Verify the 45s total timeout fires.
  Verify the caller (SearchManager) receives `SearchCommandFailed` within a
  reasonable time.

- [ ] **D.1.3** `[actor]` Queue full (64 slots) — callers notified
  Send 65+ concurrent search requests. Verify the 65th receives `QueueFull`
  (not hung). Verify search workers retry with backoff.

- [ ] **D.1.4** `[e2e]` Recovery after outage
  Block MVW traffic, trigger searches (expect failures), unblock traffic.
  Verify searches resume normally without manual intervention.

### D.2 TVDB/TMDB Down

- [ ] **D.2.1** `[e2e]` TVDB down — scoring works without enrichment
  Block TVDB API. Trigger Sonarr search. Verify results are returned to Sonarr
  (without enrichment data). Check scoring history shows entries without
  enrichment traces.

- [ ] **D.2.2** `[e2e]` TMDB down — movie scoring works without enrichment
  Block TMDB API. Trigger Radarr search. Verify results returned.

- [ ] **D.2.3** `[unit]` Circuit breaker opens and closes
  Mock TVDB/TMDB to fail. Verify circuit breaker opens (25% failure ratio,
  30s break). After 30s, verify one probe request is sent. On success, verify
  the breaker closes.

- [ ] **D.2.4** `[unit]` Rate limited (429) — Retry-After honored
  Mock TVDB/TMDB to return 429 with `Retry-After: 5`. Verify the resilience
  handler waits 5 seconds before retrying (capped at 60s).

- [ ] **D.2.5** `[e2e]` API key missing — startup validation fails
  Remove the TVDB/TMDB API key from configuration. Start the container. Verify
  the health check at `GET /api/system/setup` reports the missing key.

### D.3 Sonarr/Radarr Unreachable

- [ ] **D.3.1** `[e2e]` Setup wizard test connection — clear error
  Stop Sonarr. In the setup wizard, enter the Sonarr URL and API key. Click
  "Indexer erstellen". Verify a clear connection error is shown.

- [ ] **D.3.2** `[e2e]` Download completed but Sonarr down
  Complete a download, then stop Sonarr. Verify the MKV file remains in
  `complete/` and is importable when Sonarr comes back.

- [ ] **D.3.3** `[e2e]` Sonarr sends search but goes down afterward
  Trigger a search from Sonarr, then immediately stop Sonarr. Verify FunkArr
  completes the download regardless (it does not need Sonarr after the initial
  search request).

### D.4 GitHub Unreachable (RuleSet Updates)

**File**: `src/FunkArr.RuleSet/Updater/RuleSetUpdater.cs`.

- [ ] **D.4.1** `[actor]` GitHub API timeout — rulesets unchanged
  Mock GitHub to time out. Trigger `CheckForUpdates`. Verify existing rulesets
  remain loaded and functional. Verify `UpdateErrors` is incremented.

- [ ] **D.4.2** `[actor]` Rate limited (non-success HTTP) — retry cycle
  Mock GitHub to return 403 (rate limit). Verify the updater logs a warning,
  re-arms the 30-minute timer, but does NOT increment `UpdateErrors` (current
  behavior — this is a potential issue: `[Finding #11]`).

- [ ] **D.4.3** `[actor]` Pinned version not found
  Configure `Version = "v99.99.99"`. Verify a warning is logged every 30-minute
  cycle and existing rulesets stay in use.

### D.5 FFmpeg Issues

- [ ] **D.5.1** `[e2e]` FFmpeg binary missing — health check fails
  Remove or rename the ffmpeg binary inside the container. Verify
  `GET /api/system/setup` reports the FFmpeg check as failed.

- [ ] **D.5.2** `[manual]` FFmpeg hangs on stalled stream
  Provide a URL that accepts the connection but sends data very slowly (e.g.,
  1 byte/minute). Observe the download worker behavior. Currently there is no
  explicit ffmpeg/HTTP timeout — the download may hang indefinitely.

- [ ] **D.5.3** `[actor]` FFmpeg crash mid-remux
  Mock `IRemuxer` to throw an exception after starting. Verify the worker
  transitions to Failed, frees the slot, records history, and leaves no
  orphaned processes.

---

## Part E: Cross-Domain Integration

Tests for interactions that span multiple domains.

### E.1 Concurrent Operations

- [ ] **E.1.1** `[e2e]` Two Sonarr searches + Feed crawl simultaneously
  Trigger two episode searches from Sonarr and a feed crawl at the same time.
  Verify all three complete without errors. Check for MVW queue saturation
  and verify no silent data loss.

- [ ] **E.1.2** `[actor]` Search while MVW queue near-full + Feed crawl
  Fill the MVW queue to 60/64 slots with search requests. Start a feed crawl.
  Verify the crawl handles QueueFull gracefully (currently it silently drops
  items — `[Finding #6]`).

- [ ] **E.1.3** `[e2e]` Ruleset edit while scoring is running
  Trigger a long-running search (multiple episodes). While scoring is in
  progress, edit the matching ruleset via the UI. Verify the search completes
  with the original ruleset version (or the new one — but not a crash).

- [ ] **E.1.4** `[e2e]` Delete ruleset while search uses it
  Start a search for a specific ruleset. Delete that ruleset while the search
  is in progress. Verify no crash and a clear error or graceful fallback.

- [ ] **E.1.5** `[actor]` Community update while local overlay exists
  Simulate a community ruleset update (new version) while a local overlay
  exists for that ruleset. Verify the merge produces the correct result.

### E.2 Full Pipeline

- [ ] **E.2.1** `[e2e]` Complete pipeline: Feed crawl -> RSS sync -> Download -> Import
  This extends E2E-TEST-PLAN.md section 35 with explicit verification of every
  intermediate step:
  1. Feed crawl completes with entries published
  2. Newznab RSS returns those entries
  3. Sonarr RSS sync grabs entries
  4. FunkArr downloads complete
  5. Sonarr imports successfully

- [ ] **E.2.2** `[e2e]` Pipeline with download failure and retry
  Same flow as E.2.1, but configure the download URL to fail once (transient).
  Enable retries. Verify the retry succeeds and Sonarr imports the file.

- [ ] **E.2.3** `[e2e]` Pipeline with enrichment timeout
  Same flow, but block TVDB during the scoring phase. Verify the pipeline
  completes (without enrichment) and Sonarr still imports.

- [ ] **E.2.4** `[e2e]` Pipeline with variant exclusion
  Ensure the feed contains Audio Description entries. Verify they are excluded
  from Newznab results (not grabbed by Sonarr) and show the exclusion badge
  in the scoring detail.

### E.3 State Consistency

- [ ] **E.3.1** `[api]` Dashboard stats match actual data
  Query all API endpoints. Verify:
  - Dashboard download count matches `GET /api/downloads/history/stats`
  - Dashboard ruleset count matches `GET /api/rulesets` length
  - Dashboard storage matches `GET /api/system/storage`

- [ ] **E.3.2** `[api]` Scoring history count matches entries
  For each ruleset with history: `GET /api/rulesets/{id}/history?limit=1`.
  Verify `totalCount` matches the actual number of entries when fetched with
  a large limit.

- [ ] **E.3.3** `[api]` Feed entry count matches store
  `GET /api/feed`. Verify `total` matches `entries.length` when fetched with
  `limit=500`.

- [ ] **E.3.4** `[api]` Download queue count matches manager
  `GET /api/downloads/queue`. Cross-check with `GET /download/api?mode=queue`
  (SABnzbd). Verify counts match.

---

## Part F: Security and Input Validation

Beyond what E2E-TEST-PLAN.md already covers (SSRF prevention, API key redaction).

### F.1 Path Traversal `[Finding #19]`

**File**: `src/FunkArr.Core/Data/DataPaths.cs`.

- [ ] **F.1.1** `[unit]` Title with `../` does NOT escape download directory
  Create paths with title `"../../etc/passwd"`. Verify the resolved path is
  inside the configured download directory.

- [ ] **F.1.2** `[unit]` Title with path separators (`\` and `/`)
  Create paths with title `"foo/bar"` and `"foo\bar"`. Verify proper sanitization
  or rejection.

- [ ] **F.1.3** `[unit]` Title with null bytes
  Create paths with title containing `\0`. Verify no truncation or unexpected
  behavior.

- [ ] **F.1.4** `[unit]` Very long title (>255 characters)
  Create paths with a 300-character title. Verify the filename is truncated or
  hashed to fit filesystem limits.

- [ ] **F.1.5** `[unit]` Title with reserved filesystem characters
  Test with `<>:"|?*` (Windows reserved characters). Verify sanitization.

### F.2 API Input Validation

- [ ] **F.2.1** `[integration]` Ruleset ID regex enforced
  `POST /api/rulesets` with ID `"r1"` (too short), `"UPPER"` (uppercase),
  `"1start"` (starts with digit). Verify all return 400 with validation details.

- [ ] **F.2.2** `[integration]` Enum Unknown (0) rejected at API boundary
  Send requests with enum value `0` (Unknown) for priority, media type, strategy.
  Verify 400 response.

- [ ] **F.2.3** `[integration]` Negative offset/limit clamped
  `GET /api/rulesets/{id}/history?offset=-5&limit=-1`. Verify the values are
  clamped to >= 0 and the request succeeds (not 400).

- [ ] **F.2.4** `[integration]` Oversized request body
  `POST /api/rulesets` with a 10MB JSON body. Verify rejection (413 or similar).

- [ ] **F.2.5** `[integration]` NZB with 0-byte payload
  `POST /download/api?mode=addfile` with an empty file. Verify rejection with
  a clear error.

- [ ] **F.2.6** `[integration]` NZB with non-XML content
  `POST /download/api?mode=addfile` with a file containing `"not xml"`. Verify
  rejection with a clear error, not a 500.

- [ ] **F.2.7** `[integration]` Extremely large NZB file
  Upload a 50MB NZB file. Verify rejection or size limit enforcement.

### F.3 Existing Tests (Reference)

These are already covered in E2E-TEST-PLAN.md and should not be duplicated here:
- SSRF prevention (E2E 36.6a-36.6b)
- API key redaction in logs (E2E 36.10a-36.10c)
- Invalid/missing API key handling (E2E 20.7-20.8, 21.4-21.4a)

---

## Part G: Data Integrity

### G.1 Enum Roundtrips

**Files**: `*MappingExtensions.cs` in `src/FunkArr.Api/Extensions/` and domain
projects. Enum mapping uses exhaustive switch expressions.

- [ ] **G.1.1** `[unit]` Every Messages-to-Persistence enum mapping is exhaustive
  For each enum pair: verify the switch covers all members and the `_ => throw`
  arm catches additions. Test by adding a fake member and verifying the throw.
  Domains: Download, History, Scoring, Feed.

- [ ] **G.1.2** `[unit]` Every Api-to-Messages enum mapping is exhaustive
  Same pattern for API layer mappings.

- [ ] **G.1.3** `[integration]` API numeric enums return correct values
  `GET /api/rulesets/{id}` and verify enum fields (strategy, media type, etc.)
  are numeric integers, not strings. Verify the values match the enum definitions.

### G.2 Regex and String Handling

- [ ] **G.2.1** `[e2e]` Ruleset regex patterns survive API roundtrip
  `GET /api/rulesets/{id}/raw`. Note a regex pattern with backslashes.
  `PUT /api/rulesets/{id}` with modified non-regex fields only.
  `GET /api/rulesets/{id}/raw` again. Verify regex patterns are identical.
  (See E2E-TEST-PLAN.md "API Ruleset Roundtrip Pitfalls".)

- [ ] **G.2.2** `[unit]` Unicode normalization in matching
  Test scoring with NFC and NFD-normalized titles. Verify consistent matching.
  Related to `[Finding #13]` (VariantDetector NFD issue).

- [ ] **G.2.3** `[unit]` German special characters in titles and topics
  Test with titles containing ä, ö, ü, ß, Ä, Ö, Ü. Verify correct matching,
  Levenshtein distance, and display.

### G.3 Persistence Event Compatibility

**Files**: `src/FunkArr.Persistence/Events/`. Verify snapshot tests ensure
DTO stability.

- [ ] **G.3.1** `[unit]` Feed events have Verify snapshot tests
  Add `*_shape` and `*_roundtrip` tests for: `FeedCursorAdvanced`,
  `FeedRunFailed`, `FeedEntriesAdded`, `FeedEntriesExpired`, `PersistedFeedCrawlState`,
  `PersistedFeedStoreState`. Currently missing.

- [ ] **G.3.2** `[unit]` Download events complete coverage
  Verify all download events have shape and roundtrip tests: `DownloadEnqueued`,
  `DownloadDequeued`, `DownloadDispatched`, `DownloadMoved`, `DownloadSwapped`,
  `DownloadPriorityChanged`, `DownloadsPaused`, `DownloadsResumed`,
  `PersistedDownloadManagerState`. Some already exist, verify completeness.

- [ ] **G.3.3** `[unit]` History events coverage
  Verify `ScoringHistoryRecorded` and `PersistedHistoryState` have shape and
  roundtrip tests. Currently covered.

- [ ] **G.3.4** `[unit]` New events trigger test additions
  Document the convention: every new `Persisted*` record MUST have a Verify
  snapshot test before merge. Add this to the PR checklist.

---

## Part H: Performance and Load

### H.1 Download Throughput

- [ ] **H.1.1** `[e2e]` Max concurrent downloads at configured limit
  Set `ConcurrentDownloads = 2`. Queue 5 downloads. Verify exactly 2 are active
  at any time. Verify the 3 remaining are queued with correct priority ordering.

- [ ] **H.1.2** `[actor]` Queue with 50+ items — priority ordering holds
  Enqueue 50 items with mixed priorities (High, Normal, Low). Verify dispatch
  order respects priority buckets.

- [ ] **H.1.3** `[actor]` ForceStart while at capacity `[Finding #23]`
  Set `ConcurrentDownloads = 2` with 2 active downloads. ForceStart a third.
  Verify 3 are now active (ForceStart bypasses the limit). Document whether
  this is intended behavior.

- [ ] **H.1.4** `[actor]` Rapid enqueue/dequeue cycle
  Enqueue and immediately dequeue 100 items. Verify no state corruption,
  correct snapshot triggers, and manager stays responsive.

### H.2 Search Under Load

- [ ] **H.2.1** `[e2e]` 10 concurrent Newznab requests
  Send 10 simultaneous `GET /index/api?t=tvsearch&tvdbid=83214&apikey=<key>`.
  Verify all return valid results (some may timeout — verify the timeout
  response is correct XML).

- [ ] **H.2.2** `[actor]` MVW queue saturation (64 slots)
  Send 70 concurrent search requests. Verify 64 are accepted, 6 get QueueFull.
  Verify search workers retry the QueueFull responses.

- [ ] **H.2.3** `[actor]` Search timeout propagation
  Configure a 5s search timeout. Mock MVW to respond after 10s. Verify the
  search returns `SearchCommandFailed(TimeoutException)` within ~5s.

### H.3 Feed Scale

- [ ] **H.3.1** `[actor]` Store with 5000+ entries — query performance
  Populate FeedStoreManager with 5000 entries. Query with filters (MediaType,
  Channel, limit/offset). Verify response time is acceptable (<500ms).

- [ ] **H.3.2** `[actor]` Trim after long uptime
  Populate with entries spanning 30 days. Set retention to 14 days. Trigger
  trim. Verify entries older than 14 days are removed and the trim completes
  within a reasonable time.

- [ ] **H.3.3** `[actor]` Crawl with large page count
  Set `MaxPages = 20` with a full Mediathek. Verify the crawl completes within
  the configured timeouts and publishes entries.

### H.4 API Response Times

- [ ] **H.4.1** `[e2e]` Request timeouts enforced per group
  Verify each API group's timeout is enforced:
  - Downloads: 15s
  - Feed: 30s
  - Rulesets: 15s
  - System: 15s (logs/stream: disabled)
  - Mediathek: 60s
  Send a request that would exceed the timeout and verify 504 response.

- [ ] **H.4.2** `[e2e]` SSE streams under load
  Connect 10 simultaneous SSE clients to `/api/downloads/queue/stream`. Queue
  and start a download. Verify all 10 clients receive the progress updates.

- [ ] **H.4.3** `[api]` Output cache effectiveness
  Send `GET /index/api?t=tvsearch&apikey=<key>` (empty query, cacheable).
  Time the first request and a second request within 5 minutes. Verify the
  second is significantly faster (cache hit).

---

## Test Execution Notes

### Ordering

Parts can be executed independently, with these dependencies:

- **Part A**: Can be executed as bugs are fixed (each section is independent).
- **Part B**: Requires a populated system from E2E-TEST-PLAN.md execution.
  Run B.7 (container restart) LAST — it verifies state from all other tests.
- **Part C**: Can run independently. C.4 requires a running dev environment.
- **Part D**: Requires network manipulation (blocking traffic, stopping containers).
  Run after all other tests to avoid interference.
- **Part E**: Requires the full dev environment running. Run after E2E-TEST-PLAN.md.
- **Part F**: Can run independently (mostly unit/integration tests).
- **Part G**: Can run independently (unit tests + one API roundtrip test).
- **Part H**: Run last — load tests may affect system stability.

### Automation Priority

Recommended order for converting manual tests to automated tests:

1. **Part A** (Bug Verification) — highest value, prevents regressions
2. **Part F** (Security) — path traversal is critical
3. **Part B** (Persistence) — recovery correctness is fundamental
4. **Part G** (Data Integrity) — persistence event snapshots prevent silent breakage
5. **Part C** (Enrichment) — fills the largest domain coverage gap
6. **Part E** (Cross-Domain) — complex but high value
7. **Part D** (Degraded Mode) — harder to automate, valuable for reliability
8. **Part H** (Performance) — lowest priority, most value from manual runs
