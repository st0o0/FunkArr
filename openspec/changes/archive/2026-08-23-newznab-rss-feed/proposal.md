## Why

Prowlarr validates Newznab indexers by sending `?t=search` (no query) and expecting at least one `<item>` in the RSS response. FunkArr currently returns an empty channel for bare searches because `SearchCoordinator` with an empty query produces no results — there's nothing to search for without a term. This prevents adding FunkArr as an indexer in Prowlarr. Additionally, Sonarr and Radarr rely on RSS-based polling to discover new releases, which also requires a working RSS feed.

## What Changes

- Introduce an RSS feed actor that periodically queries MediathekViewWeb for content matching active rulesets and caches the results
- Wire the Newznab RSS feed endpoint (`?t=search` without query, `?t=tvsearch` without params) to serve cached RSS content instead of delegating to SearchCoordinator with an empty query
- Support Newznab pagination (`limit`/`offset`) on RSS responses
- Ensure the RSS feed returns content immediately after rulesets are loaded, enabling Prowlarr test validation to pass

## Capabilities

### New Capabilities
- `rss-feed-cache`: Periodic background actor that queries MediathekViewWeb for each active ruleset's topic, caches the aggregated results, and serves them as the Newznab RSS feed

### Modified Capabilities
- `newznab-indexer`: RSS feed requests route to the new cache instead of SearchCoordinator empty-query passthrough

## Impact

- **Actors**: New `RssFeedCoordinator` actor (or similar) as a singleton, scheduled refresh via Akka timers
- **API**: `NewznabController.HandleRssFeed` reads from the RSS cache instead of asking SearchCoordinator
- **Dependencies**: Depends on `RuleSetCoordinator` for active ruleset topics and `SearchCoordinator` pipeline for search execution
- **Network**: Periodic MediathekViewWeb queries (one per active ruleset topic, configurable interval)
- **Memory**: In-memory cache of recent search results (bounded, configurable max items)
