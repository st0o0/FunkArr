## Why

FunkArr declares `tvdbid`, `imdbid`, and `tmdbid` in its Newznab caps `supportedParams`, which tells Sonarr/Radarr/Prowlarr to use ID-based searches as their **primary** search mode. Sonarr sends `?t=tvsearch&tvdbid=83214` without any query text; Radarr sends `?t=movie&imdbid=tt1234567` the same way. These ID-only requests currently produce empty results because the search workers ignore the ID parameters, the RuleSet domain discards the `media` block from ruleset JSON, and the resolver only matches by topic/alias strings. Additionally, the MediathekViewWeb API response models are nested inside the actor (untestable), use a mismatched JSON naming policy masked by case-insensitive deserialization, and have no contract tests.

## What Changes

- Parse the `media` block from ruleset JSON (tvdbId, imdbId, tmdbId) and carry those IDs through registration into the RuleSetResolver's index
- Add ID-based resolution to RuleSetResolver — resolve by tvdbId/imdbId/tmdbId when topic/alias lookup misses
- Expand RuleSetResolved to include Topic so ID-only searches can reverse-resolve a topic for the MediathekViewWeb query
- Add ID-only search flow to TvSearchWorker and MovieSearchWorker: resolve ID → get topic → query MVW → score → respond
- Expand SearchResultItem with optional ID fields (TvdbId, ImdbId, TmdbId) so results carry IDs through to the response
- Emit `newznab:attr` for `tvdbid`, `imdb` (note: not `imdbid`), and `tmdbid` in Newznab XML search results
- Extract MediathekViewWeb API response models from MediathekViewWebManager to a standalone file with explicit `[JsonPropertyName]` attributes
- Add contract tests for MVW API response deserialization using sample JSON fixtures
- Add test coverage for ID-only search scenarios in both TvSearchWorker and MovieSearchWorker

## Capabilities

### New Capabilities

- `id-based-search`: ID-based search resolution — translating external media IDs (tvdbId, imdbId, tmdbId) from *arr requests into MediathekViewWeb topic queries via the RuleSet resolver
- `mvw-response-model`: Standalone MediathekViewWeb API response model with explicit JSON mapping and contract tests

### Modified Capabilities

- `ruleset-management`: RuleSetWorker registration now includes media IDs; RuleSetResolver supports ID-based resolution alongside topic/alias
- `tv-search`: TvSearchWorker handles ID-only searches (tvdbId/imdbId without query text) by resolving topic from RuleSet first
- `movie-search`: MovieSearchWorker handles ID-only searches (imdbId/tmdbId without query text) by resolving topic from RuleSet first
- `search-messages`: SearchResultItem gains optional TvdbId/ImdbId/TmdbId fields
- `newznab-indexer-api`: Search results emit newznab:attr elements for tvdbid, imdb, tmdbid when available

## Impact

- **Messages**: SearchResultItem, RegisterRuleSet, ResolveRuleSet, RuleSetResolved all gain new fields (non-breaking — all new fields are nullable)
- **RuleSet domain**: RuleSetMerger, RuleSetWorker, RuleSetResolver, RuleSetResolverState all modified
- **Search domain**: TvSearchWorker, MovieSearchWorker gain a second flow path for ID-only requests; MediathekViewWebManager internal models extracted
- **ArrApi adapter**: IndexerApiEndpoints.ToRss() emits additional newznab:attr elements
- **Test projects**: New test files for MVW contract tests, expanded worker tests for ID scenarios, expanded resolver tests for ID lookup
- **No persistence changes** — IDs are runtime state in the resolver, not persisted
- **No breaking changes** — all additions are backward-compatible
