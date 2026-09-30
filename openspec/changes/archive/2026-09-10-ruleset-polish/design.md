## Context

The `GET /api/rulesets` endpoint currently asks the `RuleSetResolver` for `RegisteredRuleSetsResult`, which returns `RegisteredRuleSetEntry(RuleSetId, Topic, Aliases, TvdbId, ImdbId, TmdbId)` per ruleset. The data for enrichment is spread across three actors:

- **RuleSetResolver** holds `MediaNameByRuleSetId` (resolved TMDB/TVDB names) - already in resolver state, just not included in `QueryAll()`
- **RuleSetManager** holds `KnownRuleSets` with `RuleSetPaths` (community/local paths) and can build detail with rule counts via `BuildDetail()`
- **MatchHistoryWorker** (sharded) holds scoring snapshots per ruleset - latest timestamp and match counts are derivable

## Goals / Non-Goals

**Goals:**
- Enrich `RuleSetListEntry` with `mediaName`, `ruleCount`, `sourceType`, `lastScoringRun`, `matchRate`
- Keep the list endpoint fast (the page loads all rulesets at once)
- Maintain domain isolation (no cross-domain references)

**Non-Goals:**
- Changing the RuleSet detail endpoint (it already has all this data)
- Adding pagination to the list endpoint
- Caching enrichment data (actor state is already in-memory)

## Decisions

### 1. MediaName: extend RegisteredRuleSetEntry in the resolver

**Decision**: Add `MediaName?` to `RegisteredRuleSetEntry` and update `QueryAll()` in `RuleSetResolverStateExtensions` to include it from `MediaNameByRuleSetId`.

This is a one-line change in the resolver state's `QueryAll` method. The resolver already has the data; it just wasn't projecting it.

### 2. RuleCount and SourceType: new query to RuleSetManager

**Decision**: Add a `QueryRuleSetSummaries` message to `RuleSetManager` that returns `RuleSetSummaryResult(RuleSetSummaryEntry[])` with per-ruleset `RuleCount` and `SourceType` ("community" | "local" | "merged"). The Manager computes this from `KnownRuleSets` + `IDataFiles`.

**Alternative considered**: Have the resolver carry rule count. Rejected - the resolver doesn't have access to rule configs, only identity data. Rule count requires reading the JSON files, which the Manager already does.

**Note on rule count**: The Manager must load and merge the config to count rules (same as `BuildDetail`). For the list, we don't need the full detail - we only need to know how many rules exist. However, since the RuleSet configs are JSON files that the Manager already reads, we can compute `RuleCount` from the merged config. To keep it lightweight, we load the config only to count rules, not to build the full detail tree.

### 3. LastScoringRun and MatchRate: summary query to MatchHistory

**Decision**: Add a `QueryScoringStats` message (with `RuleSetId`) to `MatchHistoryWorker` that returns `ScoringStatsResult(DateTimeOffset? LastRun, double? MatchRate)`. The worker computes this from its in-memory snapshots (latest timestamp, average match rate over recent runs).

The API endpoint will fan out to the MatchHistory shard region for each ruleset. This is the main cost - N Ask calls where N is the number of rulesets. For typical installations (10-50 rulesets), this is acceptable. The Asks run in parallel with `Task.WhenAll`.

**Alternative considered**: Aggregate stats in the resolver. Rejected - would require the MatchMagic domain to push stats to the RuleSet domain, violating domain isolation. Fan-out from the API layer respects boundaries.

**Timeout handling**: If a MatchHistory worker doesn't respond in time, that ruleset gets `null` for `lastScoringRun` and `matchRate`. The list still renders with the other data.

### 4. API endpoint: single enriched response

**Decision**: The list endpoint remains `GET /api/rulesets` but now gathers data from three actors (resolver, manager, match history). The endpoint:
1. Asks resolver for `RegisteredRuleSetsResult` (now includes `MediaName`)
2. Asks manager for `RuleSetSummaryResult` (rule counts, source types)
3. For each ruleset, asks match history shard for `ScoringStatsResult`
4. Joins the three result sets by `RuleSetId` and returns enriched `RuleSetListEntry[]`

Steps 1-2 run in parallel. Step 3 runs as parallel fan-out. Total latency is max(resolver, manager) + max(history per ruleset), both fast since all data is in-memory.

### 5. UI: enriched card layout

**Decision**: Extend the existing card layout. Below the ID and topic, show:
- Media name (if resolved, as subtitle below topic)
- Source badge (community/local/merged with color coding)
- Rule count
- Last scoring run (relative time) and match rate (percentage)

The search filter will also match against `mediaName`.

## Risks / Trade-offs

- **Fan-out to MatchHistory** - N parallel Asks could be slow if many rulesets exist. Mitigation: short timeout (3s) per ask, missing stats shown as "-". For future scaling, a dedicated summary actor could cache stats.
- **Rule count requires config loading** - The Manager must parse JSON to count rules. This happens on every list request. Mitigation: configs are small JSON files, and File I/O is cached by the OS. If it becomes a problem, the Manager can cache rule counts in state after each load.
- **Added complexity in API endpoint** - The list endpoint goes from a single Ask to three data sources. Mitigation: clear separation with a join step, well-tested.
