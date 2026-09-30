## Why

The MediathekSearchQuery builder only uses a subset of the MediathekViewWeb API capabilities. The `ByFullText` method is a workaround that broadcasts a search term across `["topic", "title"]` instead of leveraging the API's actual field structure. Several API features are completely unused: duration range filters, future-item exclusion, per-query operator control (`and`/`or`), and the `description` field for search. Additionally, the default `MaxResults = 5000` silently exceeds the API's hard cap of 1000, meaning results are truncated without the caller knowing.

## What Changes

- **Rename `ByFullText` to `Search`** — searches `["topic", "title", "description"]` instead of just `["topic", "title"]`
- **Add `QueryOperator` enum** (`And`, `Or`) exposed on entry-point methods (`ByTopic`, `Search`), defaulting to `And`
- **Add `WithDuration(int? min, int? max)`** — duration range filter mapped to `duration_min`/`duration_max` in the wire format
- **Add `ExcludeFuture()`** — sets `future: false` in the wire format to exclude not-yet-aired items
- **Fix size capping** — default reduced from 5000 to 200, `Build()` clamps to `1..1000` to match the API's enforced limit
- **Extend wire format DTOs** — `MediathekQueryItem` gains an `operator` field; `MediathekQuery` gains `duration_min`, `duration_max`, and `future` fields
- **Update all search actors** to use the new builder capabilities:
  - `TvSearchActor`: add `ExcludeFuture()`, optionally `FromChannel()` when RuleSet provides channel info
  - `TextSearchActor`: `ByFullText` → `Search`, add `ExcludeFuture()`
  - `MovieSearchActor`: `ByFullText` → `Search`, add `WithDuration(min: 2400)` and `ExcludeFuture()`
  - `BrowseActor`: add `ExcludeFuture()`

## Capabilities

### New Capabilities

_(none — all changes extend existing capabilities)_

### Modified Capabilities

- `mediathek-query-builder`: New entry point `Search`, `QueryOperator` enum, duration filter, future-exclusion, size clamping, removal of `ByFullText`
- `mediathek-gateway-worker`: Wire format translation extended for `operator`, `duration_min`/`duration_max`, `future`
- `tv-search-pipeline`: Query construction uses `ExcludeFuture()` and optionally `FromChannel()`
- `movie-search-pipeline`: Query construction uses `Search()`, `WithDuration()`, `ExcludeFuture()`
- `text-search-pipeline`: Query construction uses `Search()` instead of `ByFullText()`, adds `ExcludeFuture()`

## Impact

- **MediathekSearchQuery.cs** — Builder rewrite (breaking: `ByFullText` removed, replaced by `Search`)
- **MediathekClient.cs** — Wire DTOs extended with new fields
- **MediathekGatewayActor.cs** — `ToWireQuery` translation updated
- **TextSearchActor.cs, TvSearchActor.cs, MovieSearchActor.cs, BrowseActor.cs** — Query construction updated
- **All query-related tests** — Updated and extended
- **Not in scope**: RuleSetGeneratorActor/RuleSetActor (direct wire queries), pagination/offset, sortBy other fields
