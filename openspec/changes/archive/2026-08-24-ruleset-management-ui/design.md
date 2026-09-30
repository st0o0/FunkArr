## Context

ShowActor/MovieActor own rulesets as persistent state after the unified-media-search-architecture change. The RuleSet and Match Intelligence API endpoints are stubbed. TvdbClient uses deprecated v2 API without auth. The UI has no working data sources for the Rulesets and Matches tabs.

## Goals / Non-Goals

**Goals:**
- Migrate TVDB client to v4 with proper authentication
- Make matching work without TVDB in the hot path (RuleSet-centric)
- Provide full CRUD + test API for rulesets against ShowActor/MovieActor
- Expose auto-generation as an API-driven workflow (preview → apply)
- Surface API key status so the UI can guide users

**Non-Goals:**
- Building the Vue 3 UI components (separate change)
- Supporting TVDB subscriber PINs (only API key, no user-level auth)
- Real-time match quality streaming (polling is sufficient)
- Multi-node coordination for ruleset generation

## Decisions

### D1: TVDB v4 DelegatingHandler for token management

**Decision**: Implement a `TvdbAuthHandler : DelegatingHandler` that acquires a JWT via `POST /v4/login`, caches it for 24 hours, and attaches `Authorization: Bearer {token}` to all outgoing requests.

**Rationale**: The token is valid for 1 month, but caching for 24h is conservative and avoids stale-token edge cases. A DelegatingHandler keeps auth logic out of the client methods. The handler can be registered in the HttpClient pipeline via `AddHttpMessageHandler<TvdbAuthHandler>()`.

**Alternative considered**: Token refresh in each client method. Rejected because it duplicates logic and couples auth to business code.

### D2: RuleSet-centric matching — TVDB out of the hot path

**Decision**: When ShowActor has rules and receives a `Match` message, it evaluates items against rules using regex extraction only. TVDB episode data is no longer fetched or validated during matching. The S/E numbers extracted by regex ARE the match result.

**Rationale**: The RuleSet already encodes how to extract season/episode from Mediathek titles. Validating against TVDB episodes was a correctness check, but the regex patterns in community rulesets are already validated during curation. Removing the TVDB dependency from the match path means:
- No API key needed for basic operation
- Lower latency (no external HTTP call per match)
- No failure mode when TVDB is down

**What changes**: `ShowActor.HandleMatch` no longer needs `_episodesBySeason`. The `ResolveSearch` handler still fetches TVDB for show name + episode list (used by GeneratePreview and optional enrichment), but `Match` works without it.

**Impact on MatchedItemInfo**: Instead of wrapping a `TvdbEpisodeInfo`, the match result carries the extracted S/E numbers and constructed title directly. `MatchedEpisodeInfo` record changes.

### D3: Generate/Preview as a two-step API workflow

**Decision**: Auto-generation is exposed as `POST /generate/preview` (returns generated rules + test traces, no side effects) and `POST /generate/apply` (saves to actor). The preview step requires API keys and performs TVDB/TMDB + Mediathek queries.

**Rationale**: Separating preview from apply gives the UI a review step. The user sees what rules were generated, checks confidence and test results, and can edit before committing. This is the wizard flow: Search → Preview → (optional Edit) → Apply.

**Implementation**: The preview endpoint does NOT go through ShowActor/MovieActor — it uses TvdbClient/TmdbClient + MediathekClient + RuleSetGenerator directly in a transient service, since it's a stateless computation. The apply endpoint sends the result to ShowActor/MovieActor.ApplyLocalOverride.

### D4: RuleSet listing from Registry catalog + active actors

**Decision**: `GET /rulesets` returns community rulesets from the Registry's loaded catalog, augmented with match quality data from active ShowActors that have generated/local overrides.

**Rationale**: Fan-out to all possible ShowActors (hundreds of community rulesets) would be expensive. Instead:
- RuleSetRegistryActor holds the full community catalog in memory (already loaded at startup)
- The controller queries the Registry for the catalog, then queries only ShowActors that are known to have non-community state (tracked via a lightweight index)

**Alternative considered**: RuleSetRegistryActor maintains a full index of all known tvdbIds with their source. Simpler — the registry already loads all community files and knows their tvdbIds. For generated/local rulesets, the ShowActors report back when they create them.

### D5: API key status endpoint

**Decision**: `GET /api/v1/setup/api-keys` returns `{ tvdb: { configured: bool, valid: bool }, tmdb: { configured: bool, valid: bool } }`. Validity is checked by making a lightweight test call on startup (TVDB: `/v4/series/83214`, TMDB: `/find/tt0082096`).

**Rationale**: The UI needs to know whether to show the "Auto-Generate" option or the "Configure API keys" prompt. Checking validity once at startup (and caching the result) avoids repeated test calls.

## Risks / Trade-offs

**[Risk] TVDB v4 field name changes break existing tests** → All TvdbClient tests need rewriting. The `TvdbEpisodeInfo` record field renames propagate through MatchingEngine, Generator, and all test files. Mitigated by: doing the migration as the first task group so everything compiles before adding new features.

**[Risk] Removing TVDB validation from matching may reduce accuracy** → A regex might extract S01E08 from a title that's actually a different show's episode. Mitigated by: community rulesets already include topic/channel/duration filters that prevent cross-show matches. The regex extraction happens AFTER filter evaluation.

**[Trade-off] Preview endpoint bypasses actors** → The generate/preview call uses services directly instead of going through ShowActor. This means the preview result is not persisted. Acceptable because preview is explicitly a "try before you commit" operation.

**[Trade-off] RuleSet listing is eventually consistent** → A newly generated ruleset in a ShowActor might not appear in the list until the actor reports it. Acceptable for a management UI — the user can always navigate directly by tvdbId.
