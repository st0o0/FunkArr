## 1. TVDB v4 Migration

- [x] 1.1 Add `TvdbApiKey` to `SearchOptions` and bind from `FunkArr:Search:TvdbApiKey`
- [x] 1.2 Implement `TvdbAuthHandler` (DelegatingHandler): POST /v4/login, cache JWT 24h, attach Bearer header
- [x] 1.3 Update `FunkArrServiceSetup` to configure TvdbClient with base URL `api4.thetvdb.com/v4` and register `TvdbAuthHandler`
- [x] 1.4 Update `TvdbEpisodeInfo` field mappings: `EpisodeName←name`, `AiredSeason←seasonNumber`, `AiredEpisodeNumber←number`, `FirstAired←aired`
- [x] 1.5 Update `TvdbShowInfo` field mappings: `SeriesName←name`, aliases now array of objects
- [x] 1.6 Rewrite `GetShowAsync` for v4: `/series/{id}` + `/series/{id}/translations/deu`
- [x] 1.7 Rewrite `GetEpisodesAsync` for v4: `/series/{id}/episodes/default/deu?season={n}&page=0` with pagination
- [x] 1.8 Add `SearchSeriesAsync(query)` method: `GET /search?query={q}&type=series`
- [x] 1.9 Rewrite TvdbClient unit tests for v4 URL patterns and response shapes
- [x] 1.10 Update all references to renamed `TvdbEpisodeInfo` fields across codebase (MatchingEngine, Generator, ShowActor, tests)

## 2. RuleSet-Centric Matching (ShowActor Refactor)

- [x] 2.1 Refactor `ShowActor.HandleMatch` to work without TVDB episodes — regex extraction produces S/E match directly
- [x] 2.2 Update `MatchedItemInfo` to carry extracted S/E numbers and constructed title instead of `TvdbEpisodeInfo`
- [x] 2.3 Remove `MatchedEpisodeInfo` record (replaced by updated `MatchedItemInfo`)
- [x] 2.4 Update `RuleSetMatchingEngine.EvaluateRules` to return `MatchedItemInfo` without TVDB dependency for S/E strategies
- [x] 2.5 Keep TVDB episode enrichment as optional path in ShowActor (when episodes cached, add episode name to result)
- [x] 2.6 Update `SearchRequestActor` to handle updated `MatchedResults` format
- [x] 2.7 Update unit tests for ShowActor matching without TVDB episodes

## 3. ShowActor/MovieActor New Messages

- [x] 3.1 Add `GetRuleSet` message + handler to ShowActor — responds with full RuleSetFile + source + match quality
- [x] 3.2 Add `TestRules` message + handler to ShowActor — queries Mediathek via gateway, evaluates rules, returns traces
- [x] 3.3 Add `RemoveLocalOverride` message + handler + `LocalOverrideRemoved` event to ShowActor
- [x] 3.4 Add `GetRuleSet`, `TestRules`, `RemoveLocalOverride` messages + handlers to MovieActor
- [x] 3.5 Add unit tests for new ShowActor messages
- [x] 3.6 Add unit tests for new MovieActor messages

## 4. API Key Management

- [x] 4.1 Add `ApiKeyValidationService` — tests TVDB/TMDB keys at startup, caches results
- [x] 4.2 Add `GET /api/v1/setup/api-keys` endpoint to SetupController returning `ApiKeyStatus`
- [x] 4.3 Update `SearchOptionsValidator` — warn on missing keys, don't fail validation
- [x] 4.4 Add unit tests for ApiKeyValidationService and endpoint

## 5. RuleSet Generation API

- [x] 5.1 Add `RuleSetGenerationService` — stateless service using TvdbClient/TmdbClient + MediathekClient + RuleSetGenerator
- [x] 5.2 Implement `GeneratePreviewAsync(type, query, tvdbId?, imdbId?)` — search TVDB/TMDB, query Mediathek, generate rules, return preview
- [x] 5.3 Add `POST /api/v1/generate/preview` endpoint — validates API keys, calls service, returns preview response
- [x] 5.4 Add `POST /api/v1/generate/apply` endpoint — sends ruleset to ShowActor/MovieActor.ApplyLocalOverride
- [x] 5.5 Add API contract types: `GeneratePreviewRequest`, `GeneratePreviewResponse`, `GenerateApplyRequest`
- [x] 5.6 Add unit tests for RuleSetGenerationService and endpoints

## 6. RuleSet Management API (Revive Stubs)

- [x] 6.1 Implement `GET /api/v1/rulesets` — query RuleSetRegistryActor catalog, return summaries
- [x] 6.2 Implement `GET /api/v1/rulesets/{tvdbId}` — ask ShowActor.GetRuleSet
- [x] 6.3 Implement `PUT /api/v1/rulesets/{tvdbId}` — send ShowActor.ApplyLocalOverride
- [x] 6.4 Implement `DELETE /api/v1/rulesets/{tvdbId}` — send ShowActor.RemoveLocalOverride
- [x] 6.5 Implement `POST /api/v1/rulesets/{tvdbId}/test` — send ShowActor.TestRules
- [x] 6.6 Implement movie ruleset endpoints: `GET/PUT/POST /api/v1/rulesets/movies/{imdbId}`
- [x] 6.7 Add `RuleSetResponse`, `RuleSetDetail` contract types with match quality fields
- [x] 6.8 Add unit tests for RulesetController endpoints

## 7. Match Intelligence API (Revive Stubs)

- [x] 7.1 Implement `GET /api/v1/matches/topics` — aggregate GetMatchQuality from known ShowActors
- [x] 7.2 Implement `GET /api/v1/matches/topics/{topic}` — lookup ShowActor by topic via registry
- [x] 7.3 Implement `GET /api/v1/matches/unmatched` — aggregate unmatched from ShowActors
- [x] 7.4 Add unit tests for MatchIntelligenceController endpoints

## 8. API Contract Updates

- [x] 8.1 Add `ApiKeyStatus` contract type
- [x] 8.2 Add `movieTitleMatch` and `movieOriginalTitleMatch` to `RuleStrategy` contract enum
- [x] 8.3 Update OpenAPI spec with all new/changed endpoints
- [x] 8.4 Regenerate `Contracts.g.cs` from OpenAPI spec

## 9. Integration Verification

- [x] 9.1 Verify `dotnet build` succeeds with no warnings
- [x] 9.2 Verify `dotnet format` passes
- [x] 9.3 Verify all tests pass
- [x] 9.4 End-to-end test: TV search works without API keys (community ruleset, no TVDB validation)
- [x] 9.5 End-to-end test: Generate preview returns rules when API keys configured
- [x] 9.6 End-to-end test: RuleSet CRUD endpoints work against ShowActor
