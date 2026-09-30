## 1. Messages - New and extended records

- [x] 1.1 Add `MediaName` (string?) to `RegisteredRuleSetEntry` in `FunkArr.Messages/RuleSet/QueryRegisteredRuleSets.cs`
- [x] 1.2 Add `QueryRuleSetSummaries` and `RuleSetSummaryResult(RuleSetSummaryEntry[])` with `RuleSetSummaryEntry(string RuleSetId, int RuleCount, string SourceType)` to `FunkArr.Messages/RuleSet/`
- [x] 1.3 Add `QueryScoringStats(string RuleSetId)` and `ScoringStatsResult(DateTimeOffset? LastRun, double? MatchRate)` to `FunkArr.Messages/Scoring/History/`

## 2. RuleSet domain - Resolver media name

- [x] 2.1 Update `QueryAll()` in `RuleSetResolverStateExtensions` to include `MediaNameByRuleSetId` lookup in `RegisteredRuleSetEntry`
- [x] 2.2 Update existing resolver tests to verify `MediaName` is included in `QueryAll` results

## 3. RuleSet domain - Manager summaries

- [x] 3.1 Add `ToSummaries` extension method on `RuleSetManagerState` that computes `RuleCount` (from merged config) and `SourceType` (from `RuleSetPaths`) per known ruleset
- [x] 3.2 Add `Receive<QueryRuleSetSummaries>` handler in `RuleSetManager` that responds with summary result
- [x] 3.3 Add tests for `ToSummaries` in RuleSet tests (source type determination, rule count, config load failure)

## 4. MatchMagic domain - Scoring stats

- [x] 4.1 Add `ToScoringStats` method on `MatchHistoryWorker` state computing last run timestamp and average match rate
- [x] 4.2 Add `Receive<QueryScoringStats>` handler in `MatchHistoryWorker`
- [x] 4.3 Add tests for scoring stats computation (with history, without history, zero candidates)

## 5. API model and endpoint

- [x] 5.1 Extend `RuleSetListEntry` with `MediaName` (string?), `RuleCount` (int), `SourceType` (string), `LastScoringRun` (string?), `MatchRate` (double?)
- [x] 5.2 Update `GET /api/rulesets` in `RuleSetApiEndpoints` to gather data from Resolver + Manager + MatchHistory shard, join by RuleSetId, return enriched entries
- [x] 5.3 Add API endpoint tests for enriched list response (full data, partial data with history timeout)

## 6. Frontend - RuleSet list enrichment

- [x] 6.1 Update `api/rulesets.ts` types to include new fields
- [x] 6.2 Update `RuleSetList.vue` card layout with media name subtitle, source badge, rule count, and scoring stats row
- [x] 6.3 Extend search filter to also match against `mediaName`
