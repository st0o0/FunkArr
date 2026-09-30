## Why

The RuleSet list page only shows identity data (ID, topic, aliases, media IDs). Users have to drill into each ruleset to see anything useful - source type, rule count, or whether it's been actively matching. Meanwhile, the resolver holds resolved TMDB/TVDB media names that never surface on the list page, and the MatchHistory workers know when each ruleset last scored and at what hit rate. Enriching the list entry makes the RuleSet page useful at a glance.

## What Changes

- Add `MediaName?` to `RegisteredRuleSetEntry` message and `RuleSetListEntry` API model (resolver already holds this in `MediaNameByRuleSetId`)
- Add `RuleCount` to the list entry (Manager knows this from the loaded config)
- Add `Source` type string ("community", "local", "merged") to the list entry (Manager holds `RuleSetPaths`)
- Add `LastScoringRun?` timestamp and `MatchRate?` to the list entry (requires querying MatchHistory per ruleset)
- Enrich the RuleSet list UI cards with source badge, rule count, media name, last scored, and match rate

## Capabilities

### New Capabilities
- `ruleset-list-enrichment`: Extended data on the ruleset list endpoint and enriched card presentation in the UI

### Modified Capabilities
- `ruleset-api`: List endpoint returns enriched entries with media name, rule count, source type, and scoring stats
- `ruleset-ui`: RuleSet list cards show source badge, rule count, resolved media name, last scoring run, and match rate

## Impact

- **Messages**: `RegisteredRuleSetEntry` extended with new fields; new `QueryRuleSetListSummary` message to aggregate data from Manager and MatchHistory
- **RuleSet domain**: Resolver's `QueryAll` extended to include `MediaName`; Manager needs to contribute `RuleCount` and `Source` type per ruleset
- **MatchMagic domain**: MatchHistory workers need a lightweight summary query (last timestamp + match rate per ruleset)
- **Api**: `RuleSetListEntry` model extended, endpoint mapping updated
- **UI**: `RuleSetList.vue` cards enriched with new visual elements
- **No breaking changes** - all additions to existing response shapes
