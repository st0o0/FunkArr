## 1. Persistence DTOs

- [x] 1.1 Create `ShowActorJournal.cs` in `FunkArr.Persistence/` with `ShEpisodeMatched` DTO (v, s, e, ts) and `ShMatchRateSnapshot` DTO (v, ts, mr, m, u, f, sc)
- [x] 1.2 Add `ShowActorJournalExtensions` with `ToJournal()` / `ToDomain()` for `EpisodeMatched` and `MatchRateSnapshotRecorded` domain events
- [x] 1.3 Add contract tests (Verify roundtrip) for both new DTOs

## 2. ShowActor State & Events

- [x] 2.1 Define `EpisodeMatched` and `MatchRateSnapshotRecorded` domain events in ShowActor events file
- [x] 2.2 Extend `ShowActorState` with `MatchedEpisodes` (HashSet), `LastMatchedAt` (Dictionary), `MatchRateHistory` (List capped at 90), `PerRuleRecentMatches` (Dictionary of CircularQueue)
- [x] 2.3 Add state Apply methods for new events (EpisodeMatched, MatchRateSnapshotRecorded)
- [x] 2.4 Update ShowActor recovery to register Recover handlers for new DTO types
- [x] 2.5 Update ShowActor `HandleMatch` to persist `EpisodeMatched` for newly seen episodes and update per-rule recent matches
- [x] 2.6 Update ShowActor `HandleMatch` to persist `MatchRateSnapshotRecorded` when last snapshot is older than 6 hours
- [x] 2.7 Add `GetEpisodeCoverage` and `GetMatchRateTrend` message handlers to ShowActor
- [x] 2.8 Extend `GetMatchQuality` response to include per-rule stats (hitCount, hitPercent, isDead, recentMatches)

## 3. MovieActor State & Events

- [x] 3.1 Define `EpisodeMatched` and `MatchRateSnapshotRecorded` domain events in MovieActor events file
- [x] 3.2 Extend `MovieActorState` with movie-level coverage fields (matched flag, firstMatched, lastSeen), `MatchRateHistory`, and `PerRuleRecentMatches`
- [x] 3.3 Add state Apply methods for new events
- [x] 3.4 Update MovieActor recovery to register Recover handlers for new DTO types
- [x] 3.5 Update MovieActor `HandleMatch` to persist coverage and snapshot events (parallel to ShowActor changes)
- [x] 3.6 Add `GetEpisodeCoverage` and `GetMatchRateTrend` message handlers to MovieActor

## 4. RecentMatchActor Extensions

- [x] 4.1 Extend unmatched item tracking in `RecentMatchActorState` with `SeenCount` (int) and `FirstSeen` (DateTime) per item, keyed by title+topic
- [x] 4.2 Update `RecordSearchEvaluation` handler to increment seen count for recurring unmatched items
- [x] 4.3 Add `GetById(string Id)` message handler returning a single SearchEvaluation or null
- [x] 4.4 Update snapshot DTO to include recurrence fields (backward-compatible — missing fields default to seenCount=1)

## 5. Failure Pattern Analysis Service

- [x] 5.1 Create `FailurePatternAnalyzer` service class in `FunkArr.RuleSet/` with classification logic (duration_filter, title_format, no_tvdb_match, channel_filter, content_type, unknown)
- [x] 5.2 Add pattern-specific suggestion generation logic
- [x] 5.3 Add unit tests for each failure pattern classification scenario
- [x] 5.4 Register `FailurePatternAnalyzer` in DI

## 6. API Endpoints — Backend

- [x] 6.1 Add `GET /api/v1/rulesets/{tvdbId}/coverage` to `RulesetController` (Ask ShowActor.GetEpisodeCoverage)
- [x] 6.2 Add `GET /api/v1/rulesets/{tvdbId}/trend` to `RulesetController` (Ask ShowActor.GetMatchRateTrend, optional days filter)
- [x] 6.3 Add `GET /api/v1/rulesets/{tvdbId}/failures` to `RulesetController` (fetch unmatched from RecentMatchActor, run FailurePatternAnalyzer)
- [x] 6.4 Add `GET /api/v1/rulesets/stats` to `RulesetController` (fan-out GetMatchQuality to all actors, aggregate)
- [x] 6.5 Add `GET /api/v1/rulesets/movies/{id}/coverage`, `/trend`, `/failures` to `RulesetController` (parallel movie endpoints)
- [x] 6.6 Add `GET /api/v1/matches/recent/{id}` to `MatchIntelligenceController` (Ask RecentMatchActor.GetById)
- [x] 6.7 Extend `GET /api/v1/rulesets/{tvdbId}` response to include per-rule stats (recentMatches, isDead)
- [x] 6.8 Add API contract types for coverage, trend, failures, fleet stats, and per-rule stats responses

## 7. Frontend — TypeScript Types & API Client

- [x] 7.1 Add TypeScript types for fleet stats, episode coverage, match rate trend, failure patterns, and per-rule stats responses
- [x] 7.2 Add API client functions for new endpoints (fetchFleetStats, fetchCoverage, fetchTrend, fetchFailures, fetchMatchById)

## 8. Frontend — Fleet Overview (RulesetsView Redesign)

- [x] 8.1 Create `FleetStatsBar` component with 4 stat cards (total rulesets, avg match rate, attention count, unmatched)
- [x] 8.2 Redesign `RulesetsView.vue` — replace table with enriched cards showing inline progress bar, status indicator, unmatched count, last activity
- [x] 8.3 Add sort options (match rate, name, last activity) and filter options (source, status) to the list view
- [x] 8.4 Integrate `FleetStatsBar` at the top of the rulesets view

## 9. Frontend — Ruleset Detail Redesign

- [x] 9.1 Create `RulesetStatCards` component with match rate, items evaluated, matched, unmatched, and inline sparkline from trend data
- [x] 9.2 Add sub-tab navigation to `RulesetDetail.vue` (Rules, Match History, Unmatched, Coverage)
- [x] 9.3 Extend `RuleCard.vue` with recent matched episode names list, dead rule warning badge, and enriched stats display
- [x] 9.4 Create `MatchHistoryTab` component — timeline of SearchEvaluations for this ruleset with expandable item forensics
- [x] 9.5 Create `UnmatchedTab` component — failure patterns grouped by type with suggestions and expandable item list
- [x] 9.6 Create `CoverageTab` component — episode coverage grid with per-season rows, matched/unmatched cells, hover tooltips
- [x] 9.7 Create `Sparkline` component — inline SVG sparkline for match rate trend rendering
- [x] 9.8 Integrate all sub-tab components into `RulesetDetail.vue`

## 10. Frontend — Item Forensics

- [x] 10.1 Create `ItemForensicsPanel` component — expandable panel showing full pipeline trace (filter checks with pass/fail, strategy detail with regex/title/TVDB)
- [x] 10.2 Add recurrence info display ("Seen N times since date") for unmatched items
- [x] 10.3 Integrate `ItemForensicsPanel` in MatchHistoryTab and UnmatchedTab

## 11. Frontend — Navigation & Routing

- [x] 11.1 Remove Matches tab from `App.vue` navigation bar
- [x] 11.2 Remove `MatchesView.vue` and `MatchDetailView.vue` route registrations
- [x] 11.3 Add redirect routes: `/#/matches` → `/#/rulesets`, `/#/matches/:id` → `/#/rulesets`
- [x] 11.4 Clean up unused imports and components (MatchesView, MatchDetailView can remain as files but are no longer routed)

## 12. Testing & Verification

- [x] 12.1 Add ShowActor unit tests for episode coverage tracking (first match, repeated match, recovery)
- [x] 12.2 Add ShowActor unit tests for match rate snapshot recording (throttle, cap, recovery)
- [x] 12.3 Add RecentMatchActor unit tests for recurrence tracking and GetById
- [x] 12.4 Add FailurePatternAnalyzer unit tests for all classification scenarios
- [x] 12.5 Add API contract tests for new endpoints (coverage, trend, failures, stats, match-by-id)
- [x] 12.6 Build and verify the frontend compiles without errors
- [x] 12.7 Run full test suite to verify no regressions
