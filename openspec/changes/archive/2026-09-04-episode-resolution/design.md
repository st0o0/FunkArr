## Context

FunkArr's TV search pipeline flows: Sonarr request → TvSearchWorker → RuleSetResolver (topic/ID → ruleSetId) → MediathekViewWeb query → MatchMagicManager scoring → SearchResult → Newznab XML. TVDB IDs pass through from RuleSets (registration, search commands, Newznab attributes) but nobody queries the TVDB API for episode data. Shows with `seasonAndEpisodeNumber` strategy (e.g., ZDF Magazin Royale) embed S/E directly in the release title via regex capture. Shows with `itemTitleIncludes`/`itemTitleExact` (e.g., Tatort) produce matched items with `Season=null, Episode=null` — Sonarr rejects these with "Unable to identify correct episode(s)".

Both MediathekArr (C#, PCJones) and RundfunkArr.js solve this by fetching TVDB episode lists and matching Mediathek items against them using fuzzy title comparison, airdate matching, and runtime windowing.

## Goals / Non-Goals

**Goals:**
- Episode Resolution as a separate pipeline stage between scoring and result building
- TVDB v4 API client with authentication and 12h episode data cache
- Multiple resolution strategies: fuzzy title match (Levenshtein), airdate match, runtime window
- Per-RuleSet configurable strategy and thresholds
- Soft dependency on TVDB — system degrades gracefully without an API key

**Non-Goals:**
- TMDB as primary source (follow-up after TVDB works)
- Episode Guide UI for browsing cached TVDB data
- Movie resolution (Radarr uses different identifiers)
- Automatic RuleSet generation from TVDB data
- UmlautAdaptarr-style German title normalization proxy

## Decisions

### Separate domain project (FunkArr.EpisodeGuide)

Episode resolution lives in its own domain project, not in Search or MatchMagic. Follows domain isolation: references only FunkArr.Core, communicates with other domains via Messages. The EpisodeGuideManager is a Cluster Singleton actor (same pattern as MediathekViewWebManager, MatchMagicManager).

**Alternative considered:** Embed in Search domain. Rejected — Search is responsible for querying the Mediathek and orchestrating the pipeline. Metadata enrichment (resolving Mediathek items to TVDB episodes) is a distinct concern. Mixing them would make Search depend on TVDB, violating the principle that Search only talks to Mediathek.

### Episode Resolution as pipeline stage in TvSearchWorker

The resolution happens after `ScoreCompleted` and before `ToScoredResult`. TvSearchWorker already orchestrates a multi-step pipeline (resolve ruleSet → query Mediathek → score items → build result). Adding one more `Ask` step follows the established pattern. The TvSearchWorker checks if any matched items lack Season/Episode — if so, it Asks the EpisodeGuideManager; if all items already have S/E (regex-matched shows), it skips the stage entirely.

**Alternative considered:** A separate post-processing actor that intercepts SearchCompleted. Rejected — would require re-architecting how search results flow back to the Newznab adapter. The current flow is a single Ask chain terminated by `Sender.Tell(result)`.

### Resolution strategies with priority order

Strategies are applied per-item in priority order. First confident match wins:

1. **RegexExtracted** — Season/Episode already present from MatchMagic regex capture. No TVDB lookup needed. Pass through directly.
2. **FuzzyTitleMatch** — Levenshtein similarity between the Mediathek item title (or the `constructedTitle` from MatchMagic) and TVDB episode names. Configurable threshold (default 0.7 for fuzzy, 0.95 for strict).
3. **AirdateMatch** — Cross-reference the Mediathek timestamp against TVDB episode airdates within a configurable tolerance window (default ±7 days). Handles the fact that Mediathek timestamps are publication dates, not original TV airdates.
4. **RuntimeWindow** — Compare item duration against TVDB episode runtime within ±35%. Used as tiebreaker when multiple episodes match by title or airdate, not as a standalone strategy.

When no strategy produces a confident match, the item remains unresolved and uses AiredAt-based title formatting (current behavior).

### TVDB v4 API with 12h in-memory cache

EpisodeGuideManager maintains an in-memory dictionary of `tvdbId → (episodes, fetchedAt)`. Cache key is the TVDB series ID. TTL is 12 hours (aligned with MediathekArr). The cache lives in actor state — if the actor restarts, it rebuilds lazily on the next request. No persistence needed.

TVDB v4 API flow: authenticate with API key → get Bearer token → `GET /series/{id}/episodes/default?page=0` → paginate if needed. Token is cached and refreshed on 401.

**Alternative considered:** Persist episode data to SQLite. Rejected — episode data is ephemeral reference data. The 12h cache provides sufficient hit rates for typical usage (Sonarr searches periodically, same shows). Adding SQLite persistence would increase complexity without meaningful benefit.

### Soft dependency on TVDB

If `TvdbOptions.ApiKey` is null or empty, the EpisodeGuideManager responds to `ResolveEpisodes` with an immediate pass-through (all items unresolved). TvSearchWorker treats this the same as "all items already have S/E" and proceeds normally. No errors, no warnings — the system works exactly as it did before this change, just without episode resolution for title-matched shows.

If the TVDB API is unreachable or returns errors, the EpisodeGuideManager catches the exception, logs a warning, and returns all items as unresolved. The TvSearchWorker's Ask has a 10-second timeout as additional safety.

### Per-RuleSet resolution configuration

The resolution config is an optional JSON block in the RuleSet file:

```json
{
  "topic": "Tatort",
  "resolution": {
    "strategy": "fuzzy",
    "threshold": 0.7,
    "airdateTolerance": 7
  },
  "rules": [...]
}
```

- `strategy`: `"fuzzy"` (default, threshold 0.7), `"strict"` (threshold 0.95), `"none"` (skip resolution entirely)
- `threshold`: custom similarity threshold, overrides the strategy default
- `airdateTolerance`: days tolerance for airdate matching (default 7)

When absent, defaults to `"fuzzy"` with 0.7 threshold. This means existing RuleSets gain episode resolution automatically without JSON changes.

The config flows through: RuleSet JSON → `RuleSetMerger` (parses `resolution` block into `ResolutionConfig` record) → `MatchingConfig` (gains optional `ResolutionConfig` field) → `ScoreCompleted` does NOT carry it — TvSearchWorker reads it from the `MatchingConfig` that it already has access to via the ruleSetId (Ask the MatchMagicManager or cache it locally). 

Simpler path: TvSearchWorker Asks the RuleSetManager for the resolution config when needed. The RuleSetManager already handles `QueryRuleSetDetail` — extend it with resolution config, or add a lightweight `QueryResolutionConfig(ruleSetId)` message.

### Levenshtein for fuzzy matching (no external dependency)

Hand-rolled Levenshtein distance normalized to 0-1 similarity score: `1.0 - (distance / max(len(a), len(b)))`. Both strings are pre-normalized: lowercased, Umlauts expanded (ä→ae, etc.), punctuation stripped. This matches how `ReleaseTitleBuilder` already normalizes titles.

No NuGet package needed. The algorithm is ~20 lines of code. Both MediathekArr and RundfunkArr.js use the same approach.

## Risks / Trade-offs

**TVDB API rate limiting** → 12h cache minimizes API calls. A full Sonarr season search for Tatort triggers one TVDB lookup (cached for subsequent searches). Risk is low for typical single-user FunkArr instances.

**Mediathek timestamps ≠ TVDB airdates** → The Mediathek publication date often differs from the original TV airdate by days or weeks. The configurable tolerance window (default 7 days) handles recent episodes. For re-runs ("Brüder (1997)" in Tatort), airdate matching won't work — falls back to title matching. If title matching also fails (episode name doesn't match any TVDB episode in the target season), the item remains unresolved.

**False positive title matches** → "Roomservice" might fuzzy-match a similarly-named episode from a different season. Mitigation: when TVDB returns episodes for a specific season (from the Sonarr search request), resolution is scoped to that season's episodes. Cross-season false positives are avoided.

**TVDB API key requirement** → Users need a free TVDB account and API key. The setup health check should surface a warning when no API key is configured but rulesets exist that would benefit from episode resolution. The system works without it — just without episode resolution.
