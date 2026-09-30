## Why

The current search architecture splits show identity across three independent actors (SeriesResolver, RuleSetActor, SearchActors), leading to a "first search returns empty" problem where auto-generated rulesets can only be applied on the second search attempt. The three search pipelines (TV, Movie, Text) use inconsistent matching approaches — TV uses complex ruleset matching, Movie uses simple fuzzy ShowMatcher, Text uses no matching at all — and each runs in its own ShardRegion with duplicated orchestration logic.

Unifying show/movie identity into dedicated media actors and consolidating search into a single ShardRegion eliminates the cold-start gap, enables rule-based matching for movies, and reduces architectural complexity from 5 search-related ShardRegions to 3.

## What Changes

- **BREAKING**: Remove `TvSearchActor`, `MovieSearchActor`, `TextSearchActor` as separate ShardRegions. Replace with a single `SearchRequestActor` ShardRegion that handles all search types with a unified two-phase protocol (resolve → match).
- **BREAKING**: Remove `SeriesResolver` singleton actor. Show resolution (TVDB lookup + episode cache) moves into `ShowActor`.
- **BREAKING**: Remove `MovieResolver` singleton actor. Movie resolution (TMDB lookup) moves into `MovieActor`.
- **BREAKING**: Remove `RuleSetActor` (registry + index + generation + quality tracking). Replace with:
  - `ShowActor` (sharded by tvdbId) — owns show identity, TVDB episode cache, ruleset, auto-generation, and match quality stats. Persistent, event-sourced, 6h passivation.
  - `MovieActor` (sharded by imdbId) — owns movie identity, TMDB cache, ruleset, and match quality stats. Persistent, event-sourced, 6h passivation.
  - `RuleSetRegistryActor` (singleton) — only handles community ruleset loading from GitHub Releases and pushing rules to ShowActors/MovieActors.
- **BREAKING**: Remove `MatchQualityActor` as a standalone actor. Quality tracking becomes inline state in ShowActor/MovieActor.
- Introduce two-phase search protocol: `ResolveSearch → SearchHint` (media actor returns topic, channels, duration constraints, episodes) then `Match(items) → MatchedResults` (media actor matches pre-fetched Mediathek items against its rules).
- First-search-works: when no rules exist, ShowActor/MovieActor generates rules inline and applies them immediately in the same request — no fire-and-forget, no empty results.
- Auto-generation gains access to TVDB episodes during generation, enabling real validation of generated rules against known episodes instead of heuristic confidence scoring.
- Movie search gains ruleset-based matching with movie-specific strategies (title match, original title match, year+duration filtering).

## Capabilities

### New Capabilities

- `show-actor`: Sharded persistent actor (by tvdbId) that unifies show identity (TVDB resolution + episode cache), ruleset ownership (community/generated/local layers with merge logic), inline auto-generation with episode validation, and match quality tracking. Exposes `ResolveSearch` and `Match` messages.
- `movie-actor`: Sharded persistent actor (by imdbId) that unifies movie identity (TMDB resolution), ruleset ownership, inline auto-generation, and match quality tracking. Exposes `ResolveSearch` and `Match` messages with movie-specific matching strategies.
- `search-request-actor`: Single ShardRegion replacing all three search actor regions. Thin stateless orchestrator with unified two-phase protocol: resolve search hints from media actors, query MediathekGateway, delegate matching back to media actors, then expand/score results. EntityKey schema: `tv:{tvdbId}`, `movie:{imdbId}`, `text:{queryHash}`.

### Modified Capabilities

- `ruleset-registry`: Reduced scope — no longer manages indexes, lookups, auto-generation, or match quality. Becomes a singleton that downloads community rulesets from GitHub Releases and pushes them to ShowActors/MovieActors.
- `ruleset-matching-engine`: Add movie-specific matching strategies (TitleMatch, OriginalTitleMatch, YearDurationMatch). Keep existing TV strategies unchanged.
- `ruleset-auto-generation`: Generator gains access to TVDB episodes for real validation. Movie auto-generation added with simpler strategy detection. No longer triggered via fire-and-forget message to RuleSetActor — called inline by ShowActor/MovieActor.
- `community-dataset`: Add movie rulesets to the community dataset format. Schema unchanged, but `media.type` now includes `"movie"` entries that get pushed to MovieActors.
- `sharded-message-contract`: Update ShardRegion registrations — remove TV/Movie/Text search regions, add SearchRequest/ShowActor/MovieActor regions.

## Impact

- **Code**: Major restructuring of `src/FunkArr/Search/` and `src/FunkArr/RuleSet/`. Delete SeriesResolver, MovieResolver, MatchQualityActor, TvSearchActor, MovieSearchActor, TextSearchActor. New files for ShowActor, MovieActor, SearchRequestActor, RuleSetRegistryActor.
- **Persistence**: New PersistenceIds (`show-{tvdbId}`, `movie-{imdbId}`). Old PersistenceIds (`series-resolver`, `movie-resolver`, `match-quality`) become orphaned. No migration needed (0.x version, breaking changes acceptable).
- **APIs**: No external API changes — Newznab and SABnzbd interfaces remain identical. Internal actor message contracts change entirely.
- **Tests**: All search pipeline tests, resolver tests, and ruleset actor tests need rewriting against new actor boundaries.
- **Unchanged**: MediathekGatewayActor, BrowseActor, ContentFilter, QualityExpander, ResultScorer, NewznabController, DownloadActor pipeline, all persistence DTOs for downloads.
