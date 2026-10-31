## Purpose

Defines how RSS feed pages are cached in the Newznab adapter and when the cache is invalidated.

## ADDED Requirements

### Requirement: Repeated feed pages are served from cache
The Newznab adapter SHALL cache non-empty feed pages per media type, offset and limit and SHALL answer a repeated RSS request for the same page without querying the `FeedManager`, until the feed changes.

#### Scenario: Same page twice
- **WHEN** Sonarr requests `t=tvsearch&offset=0&limit=50` twice without a feed change in between
- **THEN** the `FeedManager` SHALL receive exactly one `QueryFeed`
- **AND** both responses SHALL contain the same items

#### Scenario: Different page
- **WHEN** a cached page exists for offset 0 and Sonarr requests offset 50
- **THEN** the adapter SHALL query the `FeedManager` for offset 50

### Requirement: Feed changes invalidate the cache
The `FeedManager` SHALL send `FeedChanged` to the feed cache after persisting new entries, after persisting expired entries and after recovery. On `FeedChanged` the cache SHALL drop all pages. A page fetched before the invalidation SHALL NOT be stored after it.

#### Scenario: New releases published
- **WHEN** an ingest run adds entries to the feed
- **THEN** the next RSS request SHALL query the `FeedManager` again

#### Scenario: Query racing with a change
- **WHEN** a feed query starts, the feed changes, and the query then completes
- **THEN** its result SHALL NOT be stored in the cache

### Requirement: Only feed data is cached
The API key check and the rendering of links with base URL and API key SHALL run on every request. Empty feed results and failed feed queries SHALL NOT be cached.

#### Scenario: Wrong API key
- **WHEN** a page is cached and a request for it carries a wrong API key
- **THEN** the response SHALL be the API key error, not the cached page

#### Scenario: Empty feed
- **WHEN** the feed has no entries and the same RSS request arrives twice
- **THEN** the `FeedManager` SHALL be queried both times and both responses SHALL be the validation item
