## Why

Sonarr, Radarr and Prowlarr page through the RSS feed on every RSS sync, often requesting the same pages. Each request asks the `FeedManager` singleton and renders the page again, even though the feed only changes when an ingest run publishes releases (every 15 minutes) or retention cleanup removes entries.

## What Changes

- RSS requests on `/index/api` (`t=tvsearch|movie|search` without `q`, `tvdbid`, `imdbid`, `tmdbid`) are answered through ASP.NET Core output caching with the `NewznabFeed` policy, following the existing ruleset API pattern.
- The cache varies by `t`, `cat`, `offset`, `limit`, `apikey`, host and scheme and is tagged `feed`.
- The `FeedManager` evicts the `feed` tag after persisting new entries, after retention cleanup and after recovery, through `IFeedCacheInvalidator` (Core) implemented by the ArrApi on top of `IOutputCacheStore`. Entries also expire after 30 minutes as a safety net.
- Validation items given because the feed could not be queried are not stored.
- Searches, `t=get` and `t=caps` are not cached.

## Capabilities

### New Capabilities

- `feed-page-cache`: output caching of RSS feed pages.

### Modified Capabilities

_None._

## Impact

- **Core:** `IFeedCacheInvalidator`.
- **Feed:** `FeedManager` takes `IFeedCacheInvalidator` and calls it on every feed change.
- **ArrApi:** `NewznabFeedCache` (policy, feed request predicate, no-store marker), `OutputCacheFeedInvalidator`, registered in `AddArrApiServices`; `[OutputCache]` on the Newznab action.
- No new actor, message, persistence or configuration change.
