# Proposal

## Why

Sonarr and Radarr grab new releases automatically through their RSS sync (`t=tvsearch` / `t=movie` without query). FunkArr cannot serve that today: since #202/#206 such requests get a single validation item, so new episodes are only found through searches. RSS sync is the main way an *arr grabs new content without user action (#204).

## What Changes

- **Ingest:** a persistent `FeedIngestManager` polls MediathekViewWeb on a timer and collects entries that are new since the last completed Filmliste update, using `filmlisteTimestamp` (first appearance in the list) as cursor.
- **Classification:** new entries are matched to rulesets by topic and aliases with one `QueryRegisteredRuleSets` call per run. Entries without a ruleset are counted, not published (v1).
- **Evaluation:** each ruleset group is evaluated by the existing Search workers through new `EvaluateSeries` / `EvaluateMovies` commands, so feed and search produce identical releases (title, S/E, IDs, variant policy).
- **Gate:** only confident matches with season/episode (TV) or a TMDB/IMDB ID (movies) are published.
- **Feed store:** a persistent `FeedManager` keeps the published entries, deduplicated by a stable ID, with `FirstSeenAt` = `filmlisteTimestamp`, trimmed by retention.
- **RSS serving:** requests without search criteria are answered from the feed, ordered by `FirstSeenAt`, with a stable GUID and `pubDate = FirstSeenAt`. The validation item from #206 remains the fallback when the feed has no entries for the request's category, so the *arr indexer test keeps passing on a fresh install.
- **Mapping:** `filmlisteTimestamp` per item and `queryInfo.filmlisteTimestamp` are mapped from the MediathekViewWeb API.
- **New search source** `Feed`: feed evaluations appear in the match history and can be told apart from user searches.
- **API / UI:** `GET /api/feed`, `POST /api/feed/crawl`, and a Feed page with published entries, ingest status and a "crawl now" button.
- Options `FunkArr:Feed:*`, meter `FunkArr.Feed`, docs.

## Capabilities

### New Capabilities

- `rss-feed`: ingest of new Mediathek entries, publishing gate, feed store, serving RSS sync requests from it, and the message additions it needs.

### Modified Capabilities

_None._ The message additions (Filmliste timestamps, `SearchSource.Feed`) are specified in `rss-feed`, because the main `search-messages` spec has no `## Requirements` section and cannot take a delta. Fixing that spec is out of scope.

## Impact

- **New project** `FunkArr.Feed` (domain, references Core only) and `FunkArr.Feed.Tests`; added to the solution, host and architecture tests.
- **Messages / Persistence:** feed messages and events, `SearchSource.Feed` / `PersistedSearchSource.Feed`, Filmliste timestamps on Mediathek messages.
- **Search:** MVW timestamp mapping; `EvaluateSeries` / `EvaluateMovies` entry points in the TV and movie workers.
- **ArrApi:** RSS mode reads the feed; validation item only as fallback.
- **Api / UI:** feed endpoints and page; `Feed` as search source name.
- **Host:** two new singletons, `FeedOptions`, meter registration.
- Depends on #203 (variant policy) and #205 (`IncludeFuture`), which this branch is stacked on.
