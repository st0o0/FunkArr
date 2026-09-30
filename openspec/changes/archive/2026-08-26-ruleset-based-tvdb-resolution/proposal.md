## Why

The TV search pipeline (Sonarr → FunkArr → Mediathek) is broken for TVDB ID-based lookups. When Sonarr sends `tvsearch&tvdbid=83214`, TvSearchActor relies on SeriesResolver calling the TVDB v2 API to resolve the show name. The v2 API requires JWT authentication that was never configured, causing silent 401 failures and empty search terms — resulting in 0 matched results. Text search (`t=search&q=Tatort`) works fine (100+ results), proving the issue is purely in TVDB ID → show name resolution.

The RuleSetActor already stores TVDB ID → show name mappings (from community rulesets) in its `_byTvdbId` index but doesn't expose the name in its response. Similar projects (Rundfunkarr) solve this with a "local DB first" approach, making external APIs optional. FunkArr has all the data — it just doesn't use it at the right moment.

## What Changes

- RulesResponse includes the resolved show name from the RuleSet's MediaReference, giving TvSearchActor a name to search with even when the TVDB API is unavailable
- TvSearchActor uses the RuleSet-provided name as primary search term instead of depending on SeriesResolver
- SeriesResolver degrades gracefully when the TVDB API fails — logs a warning instead of silently returning null, and the pipeline continues with RuleSet data
- TvdbClient logs API failures explicitly instead of swallowing exceptions

## Capabilities

### New Capabilities

_None — this change modifies existing capabilities._

### Modified Capabilities

- `ruleset-registry`: RulesResponse gains a `ResolvedShowName` field populated from MediaReference.Name when a TVDB ID match is found
- `tv-search-pipeline`: Search term resolution priority changes — RuleSet name becomes primary, SeriesResolver becomes optional enrichment
- `series-resolver`: Graceful degradation when TVDB API is unreachable (warn + continue instead of silent null)

## Impact

- **Actors**: TvSearchActor, RuleSetActor, SeriesResolver
- **Messages**: RulesResponse (new field), GetRulesForTopic handler (populate name)
- **API surface**: No external API changes — Newznab endpoints remain identical
- **Dependencies**: No new dependencies. Reduces hard dependency on TVDB v2 API
- **Tests**: Existing TvSearchActor and RuleSetActor tests need updates for new RulesResponse shape
