## 1. Data Model Redesign

- [x] 1.1 Replace `Filter` record with `FilterNode` (leaf) and `FilterGroup` (composite: all/any/not lists of FilterNode | FilterGroup)
- [x] 1.2 Add `Confidence` (double?, nullable) to `Rule` record, falling back to file-level confidence when null
- [x] 1.3 Add `CaptureGroup` (int?, nullable, default last) to `Rule` and `TitleRule` records
- [x] 1.4 Add `Aliases` (IReadOnlyList<string>, default empty) to `RuleSetFile` record
- [x] 1.5 Add `TmdbId` (int?, nullable) to `MediaReference` record
- [x] 1.6 Add `OverrideConfig` record (Mode: merge|replace, Add: Rule[], Remove: int[]) and optional `Overrides` field to `RuleSetFile`
- [x] 1.7 Add `channel` and `timestamp` as valid filter fields, add `eq` and `notContains` to FilterOp enum
- [x] 1.8 Update JSON serialization/deserialization for all new and changed records
- [x] 1.9 Update `RuleSetModelTests` for new fields, filter groups, and backward compatibility

## 2. Community Parser Migration

- [x] 2.1 Update `CommunityRuleSetParser` to map flat filter arrays into `FilterGroup.All` wrapper
- [x] 2.2 Preserve `TmdbId` from upstream media object in `MediaReference`
- [x] 2.3 Map legacy `"LowerThan"` and other compat shims into new FilterOp values
- [x] 2.4 Update `CommunityRuleSetParserTests` for FilterGroup output and TmdbId preservation

## 3. Matching Engine — Filter Tree Evaluation

- [x] 3.1 Replace `AllFiltersPass` with recursive `EvaluateFilterGroup` supporting all/any/not composition
- [x] 3.2 Implement `channel` field resolution in `GetFieldValue` (case-insensitive)
- [x] 3.3 Implement `timestamp` field resolution in `GetFieldValue` (unix timestamp to date comparison)
- [x] 3.4 Support configurable capture group in `BuildTitle` and regex extraction (default: last group)
- [x] 3.5 Update `RuleSetMatchingEngineTests` for composite filters, channel filter, capture group selection

## 4. Match Trace Emission

- [x] 4.1 Define `MatchTrace` record hierarchy: `MatchedTrace`, `FilteredTrace`, `UnmatchedTrace` with per-rule failure details
- [x] 4.2 Define `MatchRecord` record: id, timestamp, search params, totalResults, matched/filtered/unmatched lists with traces
- [x] 4.3 Modify `RuleSetMatchingEngine.EvaluateRules` to return `MatchTrace` per item instead of just the match result
- [x] 4.4 Add trace emission to each strategy method (record which rule/filter/reason caused the outcome)
- [x] 4.5 Unit tests for trace content: matched item trace includes rule index + strategy, filtered trace includes filter details, unmatched trace includes per-rule failures

## 5. Match Ledger Actor

- [x] 5.1 Create `MatchLedgerActor` as `ReceiveActor` with `CircularQueue<MatchRecord>` (Servus.Core)
- [x] 5.2 Handle `RecordMatchResult` message: store MatchRecord in circular queue
- [x] 5.3 Handle `GetRecentMatches` query: return last N records (default 50)
- [x] 5.4 Handle `GetTopicStats` query: compute per-topic aggregates (searchCount, matchRate, perRuleHitCounts)
- [x] 5.5 Handle `GetAllTopicStats` query: return stats for all topics, sorted by matchRate ascending
- [x] 5.6 Handle `GetUnmatchedItems` query: return unmatched items grouped by topic, optional topic filter
- [x] 5.7 Register `MatchLedgerActor` in `FunkArrActorSystemSetup`
- [x] 5.8 Add `FunkArr__MatchLedgerCapacity` option to `FunkArrOptions` with default 10000
- [x] 5.9 Unit tests for ledger actor: record storage, eviction at capacity, stats computation, query responses

## 6. SearchActor Integration

- [x] 6.1 Modify `SearchActor` to collect `MatchTrace` results from matching engine
- [x] 6.2 Build `MatchRecord` from search context + traces and tell to `MatchLedgerActor`
- [x] 6.3 Emit match events for both ruleset-based and generic pipeline searches (source="ruleset" vs source="generic-pipeline")
- [x] 6.4 Add Serilog Debug-level logging for match events (for Loki correlation)

## 7. Registry — Alias and Merge Support

- [x] 7.1 Add alias index (`Dictionary<string, RuleSetFile>`) populated alongside topic index at load time
- [x] 7.2 Update `HandleGetRulesForTopic` to check alias index when primary topic lookup fails
- [x] 7.3 Implement merge-mode override resolution: load base layer, apply add/remove from local override
- [x] 7.4 Log warnings for alias conflicts across rulesets
- [x] 7.5 Update alias and merge indexes on community refresh
- [x] 7.6 Unit tests for alias resolution, merge-mode overrides, and alias conflict handling

## 8. Auto-Generation — New Format Output

- [x] 8.1 Update `RuleSetGeneratorActor` to emit `FilterGroup` with `all` + `not` (accessibility) instead of flat filter list
- [x] 8.2 Set per-rule `Confidence` on generated rules instead of only file-level
- [x] 8.3 Update `RuleSetGeneratorTests` for new format output

## 9. Match Intelligence API

- [x] 9.1 Create `MatchIntelligenceEndpoints` static class with route registration method
- [x] 9.2 Implement `GET /api/matches/recent?limit=50` — ask MatchLedgerActor, return JSON
- [x] 9.3 Implement `GET /api/matches/topics` — ask for all topic stats, return JSON sorted by matchRate ascending
- [x] 9.4 Implement `GET /api/matches/topics/{topic}` — ask for single topic stats + recent matches, return 404 if no data
- [x] 9.5 Implement `GET /api/matches/unmatched?topic=optional` — ask for unmatched items, return JSON grouped by topic
- [x] 9.6 Apply `ApiKeyFilter` to all match intelligence endpoints
- [x] 9.7 Register match intelligence endpoints in `FunkArrApplicationSetup`
- [x] 9.8 Integration tests for each endpoint (response structure, authentication, empty ledger)

## 10. Configuration and Documentation

- [x] 10.1 Add `MatchLedgerCapacity` to `appsettings.json` with default 10000
- [x] 10.2 Document new environment variables in `docker-compose.example.yml` comments
- [x] 10.3 Add new rule format example to `appsettings.Development.json` comments or a sample file
