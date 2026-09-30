## Why

The `RssFeedCoordinator` maintains its own timer, cache, and dependency on `RuleSetCoordinator` to produce RSS feed results — duplicating infrastructure that the `TextSearchPipeline` already provides. MediathekViewWeb returns the latest content across all broadcasters when called with an empty queries array (`Queries = []`), which `SearchChildHelpers.SearchMediathekAsync` already handles for blank search terms. Routing empty Newznab queries through the normal search pipeline eliminates an entire actor, its configuration, and 60 scatter-gather requests per refresh cycle in favor of a single cached pipeline call.

## What Changes

- **BREAKING** Remove `RssFeedCoordinator` actor and all supporting infrastructure (`RssFeedOptions`, actor registration, options binding, appsettings section)
- Remove RSS special-case branching in `NewznabController` (`HandleRssFeed` method, conditional checks in `HandleTvSearch` and `HandleTextSearch`)
- Route empty search queries (`?t=search` without `q`, `?t=tvsearch` without `tvdbid`/`q`) through `SearchRouter` → `TextSearchPipeline` with an empty query string
- Apply `limit`/`offset` pagination to search results in the controller (TextSearchPipeline returns all results, controller slices)
- Remove `RssFeedCoordinator` tests, update `NewznabController`-related tests

## Capabilities

### New Capabilities

_None_

### Modified Capabilities

- `newznab-indexer`: RSS feed behavior changes from dedicated actor cache to normal search pipeline flow with empty query
- `text-search-pipeline`: Must handle empty query as a valid search (returns latest content), confirm caching and sharding behavior with empty entity key
- `rss-feed-cache`: **REMOVED** — capability no longer exists, replaced by TextSearchPipeline empty-query caching

## Impact

- **Deleted files**: `RssFeedCoordinator.cs`, `RssFeedOptions.cs`, `RssFeedCoordinatorTests.cs`
- **Modified files**: `NewznabController.cs`, `FunkArrActorSystemSetup.cs`, `FunkArrServiceSetup.cs`, `appsettings.json`
- **API behavior**: RSS responses may differ slightly (unfiltered latest content vs. ruleset-topic-filtered content) — this is intentional, Sonarr/Radarr filter by their own series list
- **No persistence impact**: RssFeedCoordinator was ephemeral (in-memory cache only)
