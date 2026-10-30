# Proposal

## Why

Sonarr and Radarr run their RSS sync, and their "Test" button when an indexer is saved, as `t=tvsearch` / `t=movie` without `q` and without any media ID (`HttpIndexerBase.FetchRecent` / `TestConnection`). FunkArr forwards these requests as a live search, which is broken in both directions (#202):

- **TV:** the worker queries the newest 50 MediathekViewWeb items across all shows, resolves the ruleset from the topic of the **first** item only, and scores all 50 items against that one ruleset. The response contains at best a few items of one random show, and items that were scored against the wrong ruleset can carry wrong season/episode attributes, which Sonarr may grab automatically.
- **Movie:** the worker rejects a search without query and IDs (`SearchMovieFailed`), so Radarr receives Newznab error XML on every RSS sync and on the indexer test.

Returning an empty result instead is not an option: Sonarr's indexer test fails when the RSS request returns no releases ("no results in the configured categories"), which would break adding FunkArr as an indexer and FunkArr's own auto-configuration.

The real RSS feed is tracked in #204. Until it exists, requests without search criteria must be answered safely and deterministically.

## What Changes

- `NewznabSearchService` detects RSS-sync requests: `t=tvsearch`, `t=movie` or `t=search` with an empty `q` and none of `tvdbid`, `imdbid`, `tmdbid`. `season` and `ep` alone do not count as search criteria.
- Such requests no longer reach the Search domain. They are answered with a single synthetic validation item, in a category matching the request, so the *arr indexer test passes.
- The validation item cannot cause a download: its title does not map to a real series or movie, and its `get` link carries an ID that `t=get` rejects with Newznab error 201.
- Behavior for requests with a query or a media ID is unchanged.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `newznab-indexer-api`: adds a requirement for search requests without search criteria (RSS sync / indexer test) and how they are answered.

## Impact

- **Code:** `FunkArr.ArrApi` only — `NewznabSearchService` (RSS-sync detection, validation result), a small builder for the validation RSS item, `TimeProvider` injected into the service. No changes to Search, Scoring or other domains.
- **API:** `/index/api` responses for `t=tvsearch|movie|search` without `q` and IDs change from a live (broken) search result or error XML to one validation item.
- **Users:** RSS sync no longer grabs mis-mapped releases, and Radarr no longer reports indexer errors on RSS sync. New episodes are still not grabbed automatically via RSS until #204 lands; interactive and automatic searches are unaffected.
- **Load:** RSS syncs no longer trigger MediathekViewWeb queries, scoring and enrichment every 15 minutes per *arr instance.
