## Why

Sonarr, Radarr and Prowlarr page through the RSS feed on every RSS sync, often requesting the same pages. Each request asks the `FeedManager` singleton and rebuilds the page. The feed only changes when an ingest run publishes releases (every 15 minutes) or retention cleanup removes entries, so almost every request could be answered from memory. While the `FeedManager` is busy (recovery, persisting a large batch), RSS requests would otherwise wait on it.

## What Changes

- The Newznab adapter caches feed pages (`FeedResult` per media type, offset and limit) in memory and serves repeated requests without asking the `FeedManager`.
- The `FeedManager` sends `FeedChanged` after persisting new entries, after retention cleanup and after recovery; a `FeedCacheActor` in the adapter clears the cache on it. There is no time-based expiry.
- Empty results and feed failures are not cached, so the validation item never sticks once the feed has entries.
- The API key filter and the request-specific parts of the response (base URL, API key in links) stay outside the cache: only feed data is cached, the RSS is rendered per request.

## Capabilities

### New Capabilities

- `feed-page-cache`: in-memory caching of RSS feed pages with change-based invalidation.

### Modified Capabilities

_None._

## Impact

- **Messages:** `FeedChanged`. **Core:** `IFeedCache` actor key.
- **Feed:** `FeedManager` tells `IFeedCache` on every feed change.
- **ArrApi:** `FeedResultCache` (singleton), `FeedCacheActor`, `NewznabSearchService` reads through the cache.
- **Host:** `FeedCacheActor` registered before the feed actors.
- No persistence or configuration change.
