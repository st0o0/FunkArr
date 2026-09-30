## Context

The current search architecture distributes show identity across three independent actors: `SeriesResolver` (TVDB name + episodes), `RuleSetActor` (topic mapping + rules), and per-type search actors (orchestration + caching). This causes:

1. **First-search-empty problem**: When no ruleset exists, auto-generation fires as a background task and the first search returns empty results. Users must search again.
2. **Inconsistent matching**: TV uses complex ruleset matching, Movie uses simple `ShowMatcher` fuzzy matching, Text uses no matching. Three different code paths.
3. **5 search-related ShardRegions**: TvSearch, MovieSearch, TextSearch, plus DownloadActor and DownloadRequestActor — unnecessary sharding overhead for search.

The system is at version 0.x, so breaking changes to persistence and internal APIs are acceptable without migration.

## Goals / Non-Goals

**Goals:**
- Eliminate the first-search-empty gap by making auto-generation synchronous within the Match call
- Unify TV and Movie search behind a common two-phase protocol (ResolveSearch → Match)
- Give movies ruleset-based matching with movie-specific strategies
- Reduce search-related ShardRegions from 5 to 3 (SearchRequest, ShowActor, MovieActor + existing Download regions)
- Enable auto-generation to validate rules against TVDB episodes during generation

**Non-Goals:**
- Changing the external Newznab/SABnzbd API surfaces
- Modifying MediathekGatewayActor, BrowseActor, or the download pipeline
- Building a UI for ruleset management (future work)
- Supporting multi-node clustering (remains single-node)

## Decisions

### D1: ShowActor/MovieActor as unified media identity actors

**Decision**: Merge SeriesResolver + RuleSet ownership + MatchQuality tracking into a single `ShowActor` per tvdbId (and equivalently `MovieActor` per imdbId).

**Rationale**: These three concerns are facets of the same entity — a show's identity, matching configuration, and match performance. Keeping them unified means the ShowActor has all the context needed to generate and validate rules in a single request without cross-actor coordination.

**Alternative considered**: Keep SeriesResolver separate but add a "generate-and-apply" protocol between RuleSetActor and SeriesResolver. Rejected because it adds message round-trips and still requires cross-actor state coordination.

### D2: Two-phase search protocol (ResolveSearch → Match)

**Decision**: The SearchRequestActor fetches Mediathek items, not the ShowActor/MovieActor. The media actors only resolve search hints and match pre-fetched items.

**Rationale**: This keeps the MediathekGateway dependency out of ShowActor/MovieActor, making them pure matching/identity actors. The SearchRequestActor is the orchestrator that knows how to build queries and combine results.

**Alternative considered**: ShowActor queries MediathekGateway directly. Rejected because it couples the persistent actor to the rate-limited gateway, and would complicate testing.

### D3: Single SearchRequestActor ShardRegion with type-prefixed EntityKeys

**Decision**: One ShardRegion with EntityKeys like `tv:329324`, `movie:tt123`, `text:query` instead of three separate regions.

**Rationale**: Reduces ShardRegion overhead and centralizes all search orchestration logic. The type prefix in the EntityKey provides natural routing.

**Alternative considered**: Keep separate regions for type safety. Rejected because the regions have near-identical orchestration logic (the two-phase protocol is the same for TV and Movie).

### D4: RuleSet storage as persistent actor state

**Decision**: Generated rules and local overrides become events in ShowActor/MovieActor persistence. Only community rules remain file-based (downloaded from GitHub Releases).

**Rationale**: Event-sourcing the rules means they survive restarts without file I/O, versioning is implicit, and the merge logic is local to the actor. Community rules stay file-based because they're externally distributed.

**Alternative considered**: Keep all three layers file-based. Rejected because file-based storage requires a central actor to manage the index and pushes merge logic into a shared component.

### D5: Inline auto-generation with episode validation

**Decision**: When ShowActor has no rules during a Match call, it calls `RuleSetGenerator.Generate()` synchronously (it's pure CPU, no I/O) with both items AND episodes, applies the result immediately, persists it, and returns matched results — all in one request.

**Rationale**: The generator already runs in milliseconds (pattern analysis on 15 samples). Running it inline eliminates the fire-and-forget gap. Having episodes available during generation enables real validation: "Does this regex actually match item X to TVDB episode Y?"

**Alternative considered**: Async generation with a callback. Rejected because it reintroduces the two-search problem.

### D6: Movie matching strategies

**Decision**: Add `movieTitleMatch` and `movieOriginalTitleMatch` as new matching strategies in the RuleSetMatchingEngine, replacing the ad-hoc `ShowMatcher` class.

**Rationale**: Movies don't need season/episode matching, but they do need title matching with umlaut normalization and duration validation. Formalizing these as strategies means movie rulesets are expressible in the same `RuleSetFile` format and can be community-curated.

## Risks / Trade-offs

**[Risk] ShowActor memory footprint with many active shows** → Each ShowActor holds TVDB episodes + rules + quality stats. With 6h passivation and typical search patterns (users search a handful of shows), the active set should be small (< 50 actors). Passivation ensures cleanup.

**[Risk] Inline generation blocks the Match response** → RuleSetGenerator.Generate() is pure CPU on 15 samples with regex compilation. Benchmarked at < 50ms. Acceptable for a first-search that would otherwise return empty.

**[Risk] Community rules push at startup floods ShowActors** → 150+ community rulesets each trigger a ShowActor creation. Mitigated by: (1) messages are fire-and-forget Tells, not Asks, (2) ShowActors are lightweight until they receive a Match, (3) community push can be batched with a small delay between messages.

**[Trade-off] ShowActor persistence events grow over time** → Match quality events accumulate. Mitigated by 500-event snapshots and 7-day rolling eviction of quality records.

**[Trade-off] Generated/local rules lost if persistence is reset** → Since we're at 0.x with breaking changes acceptable, this is fine. Community rules are re-pushed on every startup regardless.
