## Why

ShowActor (430 LOC) and MovieActor (410 LOC) each mix three unrelated responsibilities: external API resolution (TVDB/TMDB calls + caching), ruleset ownership (community/generated/local layers), and match quality tracking. This creates fat event journals (full `RuleSetFile` documents persisted in `CommunityRulesApplied` events), redundant caching (each ShowActor instance manages its own TVDB cache with 24h TTL), and no centralized rate limiting for external API calls. Community rules are persisted in each ShowActor/MovieActor journal despite being re-pushed from disk on every startup — the journal copies are strictly redundant.

Extracting dedicated gateway actors for TVDB and TMDB resolution and making community rules transient in media actors reduces ShowActor/MovieActor to pure matching engines (~200 LOC each), eliminates redundant journal data, centralizes API rate limiting, and enables future schema simplification for community ruleset JSON files.

## What Changes

- **New `TvdbGatewayActor`**: Singleton event-sourced actor that owns all TVDB API access, caches show info and episodes with journaled events, handles request deduplication, and provides centralized rate limiting. No snapshots.
- **New `TmdbGatewayActor`**: Singleton event-sourced actor that owns all TMDB API access, caches movie metadata with journaled events, handles request deduplication. No snapshots.
- **`ShowActor` simplified**: Removes `TvdbClient` dependency, `ShowResolved`/`CommunityRulesApplied` events, and `EpisodesBySeason` from persistent state. TVDB data comes from gateway (ask pattern, cached transiently). Community rules held in RAM only (pushed by registry on startup/refresh). Only `RulesGenerated`, `LocalOverrideApplied/Removed`, and `MatchQualityRecorded` remain as journal events. No snapshots (low event volume).
- **`MovieActor` simplified**: Same pattern — removes `TmdbClient` dependency, `MovieResolved`/`CommunityRulesApplied` events. TMDB data from gateway, community rules transient. No snapshots.
- **`RuleSetRegistryActor` enhanced**: Hash-based diffing on community ruleset refresh — only journals and pushes rulesets that actually changed. New events: `RuleSetLoaded`, `RuleSetUpdated`, `RuleSetRemoved` (each with content hash). Replaces snapshot-based persistence with pure event replay. No snapshots.
- **`TvdbClient`/`TmdbClient` unchanged**: HTTP clients stay as-is, just consumed by gateway actors instead of media actors.
- **`SearchRequestActor` unchanged**: Still asks ShowActor/MovieActor for `SearchHint` and `Match` — the protocol is preserved.
- **BREAKING**: `ShowActor` and `MovieActor` persistence schemas change (events removed). Existing journals become incompatible. Acceptable at v0.x per project convention.

## Capabilities

### New Capabilities
- `tvdb-gateway-actor`: Singleton event-sourced actor for centralized TVDB API access with journaled caching, request deduplication, and rate limiting
- `tmdb-gateway-actor`: Singleton event-sourced actor for centralized TMDB API access with journaled caching and request deduplication

### Modified Capabilities
- `show-actor`: Remove TVDB resolution responsibility and community rules persistence; ask gateway for metadata; hold community rules transiently; remove snapshots
- `movie-actor`: Remove TMDB resolution responsibility and community rules persistence; ask gateway for metadata; hold community rules transiently; remove snapshots
- `ruleset-registry`: Add hash-based diffing for community refresh; journal individual ruleset load/update/remove events with content; remove snapshots

## Impact

- **Persistence**: ShowActor, MovieActor, and RuleSetRegistryActor journal schemas change. Existing journals must be wiped (v0.x breaking change, no migration needed).
- **Actor hierarchy**: Two new singleton actors (`TvdbGatewayActor`, `TmdbGatewayActor`) registered via Akka.Hosting.
- **DI**: `TvdbClient` and `TmdbClient` no longer injected into ShowActor/MovieActor constructors; injected into gateway actors instead.
- **Startup order**: Gateway actors must be registered before media actors can process requests. `RuleSetRegistryActor` startup push remains the mechanism for community rule distribution.
- **Test surface**: ShowActor/MovieActor tests simplify (no TVDB/TMDB mocking needed). New tests for gateway actors (event sourcing, dedup, TTL).
- **External APIs**: No behavioral change — same TVDB/TMDB endpoints, same data. Access pattern centralizes from N ShowActors to 1 gateway.
