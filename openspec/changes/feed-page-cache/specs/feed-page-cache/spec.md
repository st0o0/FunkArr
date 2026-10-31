## Purpose

Defines how RSS feed pages on the Newznab API are cached.

## ADDED Requirements

### Requirement: RSS feed pages are output-cached
The Newznab API SHALL output-cache responses to RSS requests (`t=tvsearch`, `t=movie` or `t=search` without `q`, `tvdbid`, `imdbid` and `tmdbid`) for 60 seconds, keyed by `t`, `cat`, `offset`, `limit`, `apikey`, host and scheme.

#### Scenario: Same page twice
- **WHEN** Sonarr requests `t=tvsearch&offset=0&limit=100` twice within 60 seconds with the same API key
- **THEN** the second response SHALL be served from the cache without querying the feed

#### Scenario: Next page
- **WHEN** a cached page exists for offset 0 and Sonarr requests offset 100
- **THEN** the request SHALL be a cache miss

#### Scenario: Different API key
- **WHEN** a page is cached for one API key and the same page is requested with another key
- **THEN** the request SHALL be a cache miss and go through the API key check

### Requirement: Searches and other functions are not cached
Requests with search criteria, `t=get` and `t=caps` SHALL NOT be output-cached.

#### Scenario: Text search
- **WHEN** `t=tvsearch&q=Tatort` is requested twice
- **THEN** both requests SHALL be handled by the controller
