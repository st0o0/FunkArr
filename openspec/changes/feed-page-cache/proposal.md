## Why

Sonarr, Radarr and Prowlarr page through the RSS feed on every RSS sync, often requesting the same pages. Each request asks the `FeedManager` singleton and renders the page again, even though the feed only changes when an ingest run publishes releases (every 15 minutes) or retention cleanup removes entries.

## What Changes

- RSS requests on `/index/api` (`t=tvsearch|movie|search` without `q`, `tvdbid`, `imdbid`, `tmdbid`) are answered through ASP.NET Core output caching with the `NewznabFeed` policy, following the existing ruleset API pattern.
- The cache entry lives 60 seconds and varies by `t`, `cat`, `offset`, `limit`, `apikey`, host and scheme.
- Searches, `t=get` and `t=caps` are not cached.

## Capabilities

### New Capabilities

- `feed-page-cache`: output caching of RSS feed pages.

### Modified Capabilities

_None._

## Impact

- **ArrApi:** `NewznabFeedCache` (policy + feed request predicate), registered in `AddArrApiServices`; `[OutputCache]` on the Newznab action.
- No actor, message, persistence or configuration change.
