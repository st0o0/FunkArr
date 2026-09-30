## Why

`SearchActor` (389 lines) handles dependency resolution, caching, three search paths (TV/Movie/Text), TVDB lookups, two matching systems, and trace emission in a single actor. As search types grow and the matching logic gets more sophisticated, this monolith will become the bottleneck for changes. Splitting into parent-router with child actors establishes clear boundaries and lets each search type evolve independently.

## What Changes

- `SearchActor` becomes a router: owns dependency resolution (RuleSetRegistry, MatchLedger refs), cache, and routes incoming requests to child actors
- Three new child actors: `TvSearchActor`, `MovieSearchActor`, `TextSearchActor` — long-lived, stateless, created in `PreStart`
- Internal message protocol: parent resolves rules before telling child, child returns results + optional `MatchRecord` to parent, parent caches and forwards to original sender
- Children receive dependencies via DI (MediathekClient, TvdbClient, QualityProbeService) but never touch actor refs — inter-actor coordination stays in the parent
- Depends on `shared-prefilter` (children use `ContentFilter`) and `options-decomposition` (children inject `IOptions<SearchOptions>`)

## Capabilities

### New Capabilities
- `search-routing`: Parent-child actor topology for search. SearchActor routes requests to TvSearchActor, MovieSearchActor, TextSearchActor. Internal protocol separates cache/coordination (parent) from search logic (children). Children are long-lived, stateless, supervised by parent with default restart strategy.

### Modified Capabilities

## Impact

- `FunkArr.Search.SearchActor` — shrinks to ~120 lines (routing, cache, dep resolution)
- New `FunkArr.Search.TvSearchActor` (~130 lines) — TVDB lookup, RuleSet/generic matching, trace building
- New `FunkArr.Search.MovieSearchActor` (~40 lines) — generic pipeline matching
- New `FunkArr.Search.TextSearchActor` (~30 lines) — generic pipeline matching
- New internal message types: `ExecuteTvSearch`, `ExecuteMovieSearch`, `ExecuteTextSearch`, `SearchCompleted`
- `FunkArr.Configuration.FunkArrActorSystemSetup` — no change (children are created by parent, not registered)
- `SearchActorTests` — updated for routing behavior, new tests per child actor
