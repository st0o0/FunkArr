## Context

Sonarr, Radarr, and Prowlarr use the Newznab `caps` endpoint to discover supported search parameters. FunkArr currently declares `tvdbid` for TV search and `imdbid,tmdbid` for movie search, causing *arr clients to send ID-only requests as their primary search mode. These requests are silently broken — the search workers only use the `Query` text field, and the entire ID pipeline is missing.

The ruleset JSON already contains the mapping data (`media.tvdbId`, `media.imdbId`, `media.tmdbId`), but `RuleSetMerger` discards it. The MediathekViewWeb API is text-only (no ID support), so the ID-to-topic resolution must happen in FunkArr's RuleSet domain before the MVW query.

Additionally, the MVW API response models are nested `internal sealed record` types inside `MediathekViewWebManager`, making them untestable from the test project. The JSON deserialization uses `SnakeCaseLower` naming policy, but MVW returns camelCase — this works only because `PropertyNameCaseInsensitive = true` masks the mismatch.

## Goals / Non-Goals

**Goals:**
- ID-only search requests from Sonarr/Radarr produce correct results
- RuleSetResolver supports resolution by tvdbId, imdbId, tmdbId alongside topic/alias
- Search results include ID attributes in Newznab XML for *arr matching
- MVW API response models are standalone, explicitly mapped, and contract-tested
- Test coverage for all new and modified flows

**Non-Goals:**
- No external ID lookup service (no calls to TVDB/TMDB/IMDB APIs)
- No caching of ID mappings beyond what the resolver already holds in memory
- No changes to the download pipeline or MatchMagic scoring
- No persistence of ID data — IDs are runtime state derived from rulesets

## Decisions

### Decision 1: ID resolution lives in RuleSetResolver, not search workers

The RuleSetResolver already maintains an in-memory index of topic/alias → ruleSetId. Adding a parallel index for ID-based keys (`"tvdb:83214"` → `"tatort"`) keeps all resolution logic in one place. The alternative — having search workers call an ID lookup service — would add a new actor and cross-domain dependency.

The resolver's `ResolveRuleSet` message gains optional ID fields. Resolution strategy: try topic/alias first (existing behavior), then fall back to ID-based lookup. This means an ID-only request with no topic still resolves correctly.

`RuleSetResolved` gains a `Topic` field so that ID-only searches can use it to query MVW. The resolver already knows the topic from its registration — returning it avoids a second round-trip.

### Decision 2: Search workers get a second entry path for ID-only flow

When a search command has no `Query` but has IDs, the worker reverses the flow:

1. Ask `RuleSetResolver` with the ID(s) → get `RuleSetResolved(ruleSetId, topic)`
2. Use `topic` to build the MVW query → get results
3. Score with the resolved ruleSetId → respond

When both `Query` and IDs are present, use the existing text-based flow but carry the IDs through to the result items for Newznab attribute emission.

The alternative — always resolving by ID first — would add unnecessary latency for text-based searches that already work.

### Decision 3: RuleSetMerger extracts media IDs alongside identity

`RuleSetMerger.ExtractIdentity` currently returns `(string Topic, string[] Aliases)?`. It will return an expanded tuple including optional media IDs. The `RawRuleSet` private class gains a `Media` property matching the JSON structure. Local rulesets override community media IDs (same precedence as other fields).

### Decision 4: MVW response models extracted to standalone file with explicit JSON attributes

The `MediathekApiResponse`, `MediathekApiResult`, `MediathekApiItem`, and `MediathekApiQueryInfo` records move from nested `internal` types inside `MediathekViewWebManager` to a standalone `MediathekApiModels.cs` file in `FunkArr.Search` with `internal` visibility. Each property gets explicit `[JsonPropertyName]` attributes matching MVW's actual camelCase field names. The `SnakeCaseLower` naming policy is removed and replaced with a plain `JsonSerializerOptions` using only `PropertyNameCaseInsensitive = true` as a safety net.

This makes the models testable from `FunkArr.Search.Tests` via `[assembly: InternalsVisibleTo]` (which already exists for other internal types like `MediathekQueryBuilder`).

### Decision 5: Newznab attribute naming follows the spec

The *arr ecosystem uses these attribute names in `<newznab:attr>`:
- `tvdbid` (int) — matches the query parameter name
- `imdb` (string, tt-prefixed) — different from the query parameter `imdbid`
- `tmdbid` (int) — matches the query parameter name

`IndexerApiEndpoints.ToRss()` emits these attributes on each item when the corresponding value is present in `SearchResultItem`.

### Decision 6: RegisterRuleSet carries IDs from RuleSetWorker

`RegisterRuleSet` gains `int? TvdbId`, `string? ImdbId`, `int? TmdbId`. `RuleSetWorker` already calls `RuleSetMerger.ExtractIdentity` — the expanded return value flows naturally into the registration message. No new actor interactions needed.

## Risks / Trade-offs

- **[IDs only as good as rulesets]** If a ruleset doesn't have a `media` block, ID-based search for that show/movie will fall through to `RuleSetNotFound`. This is acceptable — the user sees the same behavior as today, and adding media blocks to rulesets is incremental. → Mitigation: log a warning when a ruleset lacks media IDs so users can update their rulesets.

- **[Multiple rulesets with same ID]** Two rulesets could theoretically map the same tvdbId. The resolver uses last-write-wins (same as topic registration). → Mitigation: log a warning on collision; this is a ruleset authoring error, not a system error.

- **[MVW API contract drift]** Extracting models with explicit attributes makes the code sensitive to API changes. → Mitigation: contract tests with sample JSON fixtures detect drift at test time.

- **[ID-only search with no matching ruleset]** When Sonarr sends `tvdbid=12345` and no ruleset maps that ID, FunkArr must respond with an empty result, not an error. The *arr clients handle empty results gracefully. → Mitigation: return `SearchCompleted` with zero items instead of `SearchFailed`.
