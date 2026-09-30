## 1. Messages — Expand message records

- [x] 1.1 Expand `RegisterRuleSet` with `int? TvdbId, string? ImdbId, int? TmdbId`
- [x] 1.2 Expand `ResolveRuleSet` with `int? TvdbId, string? ImdbId, int? TmdbId` (TopicOrAlias becomes nullable)
- [x] 1.3 Expand `RuleSetResolved` with `string Topic`
- [x] 1.4 Expand `SearchResultItem` with `int? TvdbId, string? ImdbId, int? TmdbId`
- [x] 1.5 Fix all compilation errors from changed message signatures across the solution

## 2. MVW response models — Extract and contract-test

- [x] 2.1 Create `MediathekApiModels.cs` in `FunkArr.Search` with extracted response records and explicit `[JsonPropertyName]` attributes
- [x] 2.2 Update `MediathekViewWebManager` to reference the standalone models and remove `SnakeCaseLower` naming policy
- [x] 2.3 Add MVW API sample JSON fixture files in `FunkArr.Search.Tests/Resources/`
- [x] 2.4 Add `MediathekApiModelTests.cs` with contract tests: full response, empty results, partial fields, error response

## 3. RuleSet domain — Media ID extraction and ID-based resolution

- [x] 3.1 Add `RawMedia` class to `RuleSetMerger` and parse `media` block in `RawRuleSet`
- [x] 3.2 Expand `RuleSetMerger.ExtractIdentity` to return media IDs alongside topic/aliases
- [x] 3.3 Update `RuleSetWorker` to pass media IDs into `RegisterRuleSet`
- [x] 3.4 Add ID index to `RuleSetResolverState` — store/remove ID→(ruleSetId, topic) mappings on register/deregister
- [x] 3.5 Update `RuleSetResolver` to handle expanded `ResolveRuleSet` with ID fallback
- [x] 3.6 Add `RuleSetMergerTests` for media ID extraction (community, local override, standalone, no media block)
- [x] 3.7 Add `RuleSetResolverTests` for ID-based resolution (resolve by tvdb/imdb/tmdb, topic precedence, re-registration, deregistration)

## 4. Search domain — ID-only search flow

- [x] 4.1 Update `TvSearchWorkerState` to carry media IDs from the command
- [x] 4.2 Update `TvSearchWorker` with ID-only path: resolve ID → get topic → query MVW → score
- [x] 4.3 Update `TvSearchWorkerState` result mapping to include IDs on `SearchResultItem`
- [x] 4.4 Update `MovieSearchWorkerState` to carry media IDs from the command
- [x] 4.5 Update `MovieSearchWorker` with ID-only path: resolve ID → get topic → query MVW → score
- [x] 4.6 Update `MovieSearchWorkerState` result mapping to include IDs on `SearchResultItem`
- [x] 4.7 Update `MovieSearchWorker` error case: no query AND no IDs → SearchFailed with updated message
- [x] 4.8 Add `TvSearchWorkerTests` for ID-only scenarios (tvdbId-only, ID with season/ep, unresolved ID, text+ID passthrough)
- [x] 4.9 Add `MovieSearchWorkerTests` for ID-only scenarios (imdbId-only, tmdbId-only, unresolved ID, no query no ID, text+ID passthrough)

## 5. ArrApi adapter — Newznab response enrichment

- [x] 5.1 Update `IndexerApiEndpoints.ToRss()` to emit `newznab:attr` for tvdbid, imdb (not imdbid), tmdbid when present on SearchResultItem
- [x] 5.2 Add tests in `NewznabXmlTests` for ID attributes in XML response (with IDs, without IDs, mixed)

## 6. Build verification

- [x] 6.1 Run `dotnet build FunkArr.slnx` — verify zero errors
- [x] 6.2 Run `dotnet format --verify-no-changes` — fix any violations
- [x] 6.3 Run all test projects — verify all pass
