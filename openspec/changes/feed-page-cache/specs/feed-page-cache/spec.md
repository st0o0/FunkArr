## Purpose

Defines how RSS feed pages on the Newznab API are cached and invalidated.

## ADDED Requirements

### Requirement: RSS feed pages are output-cached
The Newznab API SHALL output-cache responses to RSS requests (`t=tvsearch`, `t=movie` or `t=search` without `q`, `tvdbid`, `imdbid` and `tmdbid`) under the tag `feed`, keyed by `t`, `cat`, `offset`, `limit`, `apikey`, host and scheme, with an expiry of 30 minutes.

#### Scenario: Same page twice
- **WHEN** Sonarr requests `t=tvsearch&offset=0&limit=100` twice with the same API key and no feed change in between
- **THEN** the second response SHALL be served from the cache without querying the feed

#### Scenario: Next page
- **WHEN** a cached page exists for offset 0 and Sonarr requests offset 100
- **THEN** the request SHALL be a cache miss

#### Scenario: Different API key
- **WHEN** a page is cached for one API key and the same page is requested with another key
- **THEN** the request SHALL be a cache miss and go through the API key check

### Requirement: The FeedManager invalidates the cache on every feed change
The `FeedManager` SHALL invalidate the `feed` cache tag after persisting new entries, after persisting expired entries and after recovery. A failed invalidation SHALL be logged and SHALL NOT affect the feed.

#### Scenario: New releases published
- **WHEN** an ingest run adds entries to the feed
- **THEN** the cache SHALL be invalidated and the next RSS request SHALL query the feed again

#### Scenario: Only known entries
- **WHEN** `AddFeedEntries` contains only entries already in the feed
- **THEN** the cache SHALL NOT be invalidated

### Requirement: Fallback answers are not stored
A response that falls back to the validation item because the feed could not be queried SHALL NOT be stored in the cache.

#### Scenario: Feed unavailable
- **WHEN** the feed query times out and the validation item is returned
- **THEN** the next identical request SHALL query the feed again

### Requirement: Searches and other functions are not cached
Requests with search criteria, `t=get` and `t=caps` SHALL NOT be output-cached.

#### Scenario: Text search
- **WHEN** `t=tvsearch&q=Tatort` is requested twice
- **THEN** both requests SHALL be handled by the controller
