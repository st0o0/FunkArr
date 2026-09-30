## Why

After the unified-media-search-architecture change, ShowActor/MovieActor own rulesets as persistent state, but all RuleSet and Match Intelligence API endpoints are stubbed (return empty/404). The TVDB client uses the deprecated v2 API without authentication that can break at any time. TMDB key is optional with silent failure. Users have no way to create, view, edit, or test rulesets through the UI, and auto-generation is an invisible background process with no user feedback.

## What Changes

- **BREAKING**: Migrate `TvdbClient` from v2 (unauthenticated, deprecated) to TVDB v4 API (JWT bearer token authentication, new base URL, renamed response fields)
- Add `TvdbApiKey` to `SearchOptions` configuration alongside existing `TmdbApiKey`
- TVDB/TMDB keys are NOT required for startup or basic matching with existing rulesets — only required for the generate/preview endpoint
- Refactor `ShowActor.Match` to work without TVDB episode validation when rules exist — extract S/E numbers from regex patterns directly, TVDB enrichment becomes optional for better release titles
- Add new messages to `ShowActor`/`MovieActor`: `GetRuleSet`, `GeneratePreview`, `TestRules`, `RemoveLocalOverride`
- Revive all stubbed RuleSet Management API endpoints (`/api/v1/rulesets/*`) wired to ShowActor/MovieActor
- Revive all stubbed Match Intelligence API endpoints (`/api/v1/matches/*`) wired to ShowActor/MovieActor match quality state
- Add new auto-generation API: `POST /api/v1/generate/preview` (search TVDB/TMDB + Mediathek, generate rules, return preview with test traces) and `POST /api/v1/generate/apply`
- Add API key status endpoint: `GET /api/v1/setup/api-keys`
- Update API contracts (OpenAPI spec) with new types and movie strategies

## Capabilities

### New Capabilities

- `tvdb-v4-client`: TVDB v4 API client with JWT authentication via DelegatingHandler, token caching, German translation support via `/series/{id}/episodes/default/deu`, and updated response models (field renames from v2)
- `api-key-management`: Configuration, validation, and status reporting for external API keys (TVDB v4, TMDB). Keys are optional for basic operation but required for ruleset generation features.
- `ruleset-generation-api`: HTTP API for the auto-generate workflow: preview (search external DB + Mediathek, generate rules, return test traces) and apply (save generated ruleset to ShowActor/MovieActor). Requires API keys.

### Modified Capabilities

- `show-actor`: Add `GetRuleSet`, `GeneratePreview`, `TestRules`, `RemoveLocalOverride` messages. Refactor `Match` to work without TVDB episodes — RuleSet-centric matching where S/E extraction from regex is sufficient without TVDB validation.
- `movie-actor`: Add `GetRuleSet`, `GeneratePreview`, `TestRules`, `RemoveLocalOverride` messages.
- `ruleset-api`: Revive all stubbed endpoints. Route to ShowActor/MovieActor instead of deleted RuleSetActor. Split into show endpoints (`/rulesets/{tvdbId}`) and movie endpoints (`/rulesets/movies/{imdbId}`). Add test endpoint per ruleset.
- `match-intelligence-api`: Revive all stubbed endpoints. Aggregate match quality from active ShowActors/MovieActors instead of deleted MatchQualityActor.
- `options-structure`: Add `TvdbApiKey` to `SearchOptions`. Validation: warn if keys missing but don't block startup.
- `api-contracts`: Add new contract types (ApiKeyStatus, GeneratePreviewRequest/Response, RuleSetDetail with match quality). Add movieTitleMatch/movieOriginalTitleMatch to RuleStrategy enum.

## Impact

- **Code**: Major changes to `TvdbClient` (v4 migration + DelegatingHandler), `ShowActor`/`MovieActor` (new messages + match refactor), `RulesetController`/`MatchIntelligenceController` (revive from stubs), `SearchOptions`/`SearchOptionsValidator`, API contracts.
- **Configuration**: New env var `FunkArr__Search__TvdbApiKey`. Existing `FunkArr__Search__TmdbApiKey` behavior unchanged.
- **APIs**: All internal management endpoints change (but they were stubbed, so no real consumers). Newznab/SABnzbd external APIs unchanged.
- **Tests**: TvdbClient tests need full rewrite for v4. Actor tests need updates for new messages. API controller tests needed.
- **Unchanged**: MediathekGatewayActor, BrowseActor, ContentFilter, QualityExpander, ResultScorer, download pipeline, Newznab/SABnzbd surface.
