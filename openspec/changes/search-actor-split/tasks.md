## 1. Internal Message Protocol

- [ ] 1.1 Define `ExecuteTvSearch(CacheKey, Request, SearchTerm, ShowName, Rules, ReplyTo)` internal record
- [ ] 1.2 Define `ExecuteMovieSearch(CacheKey, Request, SearchTerm, ReplyTo)` internal record
- [ ] 1.3 Define `ExecuteTextSearch(CacheKey, Request, ReplyTo)` internal record
- [ ] 1.4 Define `SearchCompleted(CacheKey, Results, MatchRecord?, ReplyTo)` internal record
- [ ] 1.5 Decide placement (nested in `SearchActor` vs. a shared internal file in `FunkArr.Search`) and keep all four types internal to `FunkArr.Search`

## 2. TvSearchActor

- [ ] 2.1 Create `TvSearchActor : ReceiveActor` in `src/FunkArr/Search/TvSearchActor.cs`, constructor takes `MediathekClient`, `TvdbClient`, `QualityProbeService`, probe limit (via `IOptions<T>` per options-decomposition)
- [ ] 2.2 Handle `ExecuteTvSearch`: run `SearchMediathekAsync` (moved from `SearchActor`), branch on `Rules.Count > 0`
- [ ] 2.3 Ruleset path: port `ApplyRuleSetMatchingWithTraces` (TVDB episode fetch, `RuleSetMatchingEngine.EvaluateRulesWithTraces`, quality probing of matched items, `MatchRecord` construction with `Source = "ruleset"`)
- [ ] 2.4 Generic path: port air-date resolution (TVDB episode lookup + `FirstAired` parse), build `MatchContext`, call `MatchingPipeline.ExecuteAsync`, build `MatchRecord` with `Source = "generic-pipeline"` (port `EmitGenericPipelineRecord` logic without the direct `_matchLedger.Tell` — return the record instead)
- [ ] 2.5 Port `GetTvdbEpisodesAsync` helper unchanged
- [ ] 2.6 Reply to `ExecuteTvSearch` sender with `SearchCompleted(CacheKey, filtered, matchRecord, ReplyTo)` — never call `_matchLedger` or `_ruleSetRegistry` directly, both must not exist as fields on this actor
- [ ] 2.7 Preserve TV show-name resolution (`ShowName ?? request.Query`) and cache-key construction exactly as today (moves to parent — verify child receives the already-computed `SearchTerm`/`ShowName`/`CacheKey`, does not recompute them)

## 3. MovieSearchActor

- [ ] 3.1 Create `MovieSearchActor : ReceiveActor` in `src/FunkArr/Search/MovieSearchActor.cs`, constructor takes `MediathekClient`, `QualityProbeService`, probe limit
- [ ] 3.2 Handle `ExecuteMovieSearch`: run `SearchMediathekAsync`, build `MatchContext { ShowName = Request.Query, ImdbId = Request.ImdbId }`, call `MatchingPipeline.ExecuteAsync`, build generic-pipeline `MatchRecord`
- [ ] 3.3 Reply with `SearchCompleted(CacheKey, filtered, matchRecord, ReplyTo)`

## 4. TextSearchActor

- [ ] 4.1 Create `TextSearchActor : ReceiveActor` in `src/FunkArr/Search/TextSearchActor.cs`, constructor takes `MediathekClient`, `QualityProbeService`, probe limit
- [ ] 4.2 Handle `ExecuteTextSearch`: run `SearchMediathekAsync` with `Request.Query`, build empty `MatchContext`, call `MatchingPipeline.ExecuteAsync`, build generic-pipeline `MatchRecord`
- [ ] 4.3 Reply with `SearchCompleted(CacheKey, filtered, matchRecord, ReplyTo)`

## 5. Shared Helper Extraction

- [ ] 5.1 Move `SearchMediathekAsync` (Mediathek query + error handling/logging) to a location reachable by all three children — either duplicated per child (if trivially small) or a shared internal static helper in `FunkArr.Search` (avoid a fourth actor/service unless duplication becomes a real maintenance cost)
- [ ] 5.2 Verify `EmitGenericPipelineRecord`-equivalent logic (building the empty-trace `MatchRecord`) is consistent across `MovieSearchActor`, `TextSearchActor`, and the generic-path branch of `TvSearchActor`

## 6. SearchActor — Router Refactor

- [ ] 6.1 Add `_tvSearchActor`, `_movieSearchActor`, `_textSearchActor` `IActorRef` fields; create all three in `PreStart` via `Props.Create`, independent of dependency resolution
- [ ] 6.2 Replace `HandleTvSearch`/`HandleMovieSearch`/`HandleTextSearch` bodies with: compute cache key, check cache (unchanged `TryGetCached`), on miss resolve show name / ask `RuleSetRegistry` for rules (TV only), build `Execute*` command with `Sender` as `ReplyTo`, `Tell` to the matching child
- [ ] 6.3 Add `Receive<SearchCompleted>` handler in `Ready`: `CacheResults(msg.CacheKey, msg.Results)`, `_matchLedger!.Tell(...)` if `MatchRecord` present, `msg.ReplyTo.Tell(new SearchResponse(msg.Results))`
- [ ] 6.4 Remove `ApplyRuleSetMatchingWithTraces`, `EmitGenericPipelineRecord`, `GetTvdbEpisodesAsync`, `SearchMediathekAsync` from `SearchActor` (now live in children/shared helper)
- [ ] 6.5 Keep `Resolving`/`Ready`/`TryBecomeReady`/`HandleTerminated`/stashing logic unchanged
- [ ] 6.6 Verify `SearchActor` external message records (`TvSearchRequest`, `MovieSearchRequest`, `TextSearchRequest`, `SearchResponse`) are untouched and still public/accessible to `RulesetEndpoints` and other callers

## 7. Tests

- [ ] 7.1 Update `SearchActorTests` (`src/FunkArr.Tests/Search/SearchActorTests.cs`) to assert routing behavior: cache hit skips child dispatch, cache miss routes to correct child, `SearchCompleted` reply is cached and forwarded to original sender, dependency stashing/re-resolution still works
- [ ] 7.2 Write `TvSearchActorTests`: ruleset path (rules present, matches produced, `MatchRecord` with `Source = "ruleset"`), generic path (no rules, air-date resolution, `MatchRecord` with `Source = "generic-pipeline"`), empty-rules fallback
- [ ] 7.3 Write `MovieSearchActorTests`: `ExecuteMovieSearch` → `MatchingPipeline` invocation → `SearchCompleted` with expected results and match record
- [ ] 7.4 Write `TextSearchActorTests`: `ExecuteTextSearch` → `MatchingPipeline` invocation → `SearchCompleted` with expected results and match record
- [ ] 7.5 Verify existing test doubles/mocks for `MediathekClient`, `TvdbClient`, `QualityProbeService` are reusable for child actor tests (extend `FunkArr.Tests.Shared` if a new shared fixture is needed)

## 8. Verification

- [ ] 8.1 `dotnet build FunkArr.slnx` from `src/` — no warnings introduced
- [ ] 8.2 `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj` — all tests pass, including new child actor tests and updated `SearchActorTests`
- [ ] 8.3 Manual sanity check: confirm `RulesetEndpoints` and any other `SearchActor` caller still compiles unmodified against the unchanged external message types
