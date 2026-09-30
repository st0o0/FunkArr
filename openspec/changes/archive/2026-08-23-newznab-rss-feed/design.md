## Context

FunkArr exposes a Newznab-compatible indexer API. Bare search queries (`?t=search` without `q`, `?t=tvsearch` without `tvdbid`/`q`) are treated as RSS feed requests per the Newznab spec. Currently these delegate to `SearchCoordinator` with an empty query, which produces zero results — MediathekViewWeb returns nothing for an empty search term.

Prowlarr tests Newznab indexers by sending `?t=search` and requires at least one `<item>` in the response. Sonarr/Radarr use RSS polling to discover new releases. Both fail with zero results.

The `RuleSetCoordinator` already holds all active rulesets with their topics and TVDB/IMDB IDs. The `SearchCoordinator` already handles search-and-cache via its 5-worker pipeline. The RSS feed needs to leverage both: use rulesets to know *what* to search for, and the search pipeline to execute and cache those searches.

## Goals / Non-Goals

**Goals:**
- RSS feed returns recent Mediathek content matching active rulesets
- Prowlarr test validation passes (at least one `<item>` when rulesets exist)
- Sonarr/Radarr RSS sync discovers new content
- Periodic background refresh without blocking API requests
- Respect Newznab pagination (`limit`/`offset`)

**Non-Goals:**
- Real-time Mediathek monitoring (periodic polling is sufficient)
- Persisting the RSS cache across restarts (in-memory is fine, rebuilds on startup)
- RSS feed without any rulesets configured (empty is acceptable — no rulesets = nothing to index)

## Decisions

### 1. RssFeedCoordinator as a singleton actor

The RSS feed cache lives in a new `RssFeedCoordinator` singleton actor. On startup and on a configurable timer (default: 30 minutes), it asks `RuleSetCoordinator` for all ruleset topics, then asks `SearchCoordinator` for each topic. Results are aggregated, deduplicated, sorted by timestamp (newest first), and stored in memory.

**Why actor over a hosted service:** Consistent with FunkArr's actor-based architecture. Can use Akka timers for scheduling, participate in supervision, and communicate with other actors via message passing. Follows the established `*Coordinator` naming convention.

**Why delegate to SearchCoordinator:** Reuses the existing pipeline (MediathekGateway → ContentFilter → QualityProbe → Score) and its 55-minute cache. No duplicate Mediathek queries if searches and RSS overlap.

### 2. Sequential topic queries with rate limiting

Topics are searched sequentially (not in parallel) to avoid overwhelming MediathekViewWeb. A short delay (1 second) between queries prevents rate limiting. SearchCoordinator's cache means most refreshes are fast (cache hits).

**Alternative considered:** Parallel queries would be faster but risks rate limiting from MediathekViewWeb, and the RSS cache doesn't need sub-second freshness.

### 3. NewznabController reads from RssFeedCoordinator

`HandleRssFeed` in `NewznabController` asks `RssFeedCoordinator` for cached results instead of `SearchCoordinator`. The coordinator returns its cached aggregated results with pagination support.

**Why not keep SearchCoordinator with empty query:** An empty query is semantically meaningless for MediathekViewWeb. The RSS feed concept is "recent content matching my rulesets" — a fundamentally different operation than "search for X".

### 4. In-memory bounded cache

The cache holds up to 500 items (configurable). Items are sorted newest-first. On refresh, old items are replaced entirely (swap, not merge). This keeps memory bounded and avoids stale entries accumulating.

## Risks / Trade-offs

- **[Empty rulesets → empty RSS]** If no rulesets are configured, the RSS feed returns empty and Prowlarr test fails. → This is acceptable: FunkArr without rulesets has nothing to index. The setup wizard should prompt for rulesets before Prowlarr integration.
- **[Startup delay]** First RSS refresh takes time (one Mediathek query per topic). → The refresh runs async on startup; API returns empty until first refresh completes. Prowlarr retests periodically.
- **[MediathekViewWeb load]** Periodic queries for all topics add load. → Rate-limited sequential queries + SearchCoordinator cache minimizes impact. Configurable refresh interval.
- **[Stale results]** 30-minute refresh means up to 30 minutes latency for new content. → Acceptable for RSS use case. Sonarr/Radarr also poll on intervals (15-60 min typically).
