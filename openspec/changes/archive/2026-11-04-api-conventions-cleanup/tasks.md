## 1. Model organization — create subfolder structure

- [x] 1.1 Create `Models/Shared/` subfolder. Move `MediaType`, `MediathekVariant`, `VariantPolicy` enums and `OperationResult`, `ErrorResponse`, `ValidationErrorResponse` records there. Update namespace to `FunkArr.Api.Models.Shared`.
- [x] 1.2 Create `Models/Downloads/` subfolder. Move `DownloadQueueResponse`, `DownloadQueueItem`, `QueueStatus`, `QueuePhase`, `QueuePriority` to `DownloadQueueResponse.cs`. Move `DownloadHistoryResponse`, `DownloadHistoryItem`, `HistoryStatus` to `DownloadHistoryResponse.cs`. Move `DownloadHistoryRequest` to its own file. Move `MoveRequest`, `PriorityRequest`, `SwapRequest` to their own files. Move `DownloadSettingsResponse`, `DownloadTimeSlotResponse` to `DownloadSettingsResponse.cs`. Update namespace.
- [x] 1.3 Create `Models/RuleSets/` subfolder. Move `RuleSetListResponse`, `RuleSetListEntry`, `SourceType` to `RuleSetListResponse.cs`. Move `RuleSetDetail`, `RuleSetDetailRule` to `RuleSetDetailResponse.cs`. Move `CreateRuleSetRequest`, `UpdateRuleSetRequest`, `IRuleSetBody` to their own files. Move `RuleSetBody` (MediaInput, RuleInput, FilterGroupInput, FilterNodeInput, TitleRuleInput) to `RuleSetBody.cs`. Move `FilterGroupOutput`, `FilterConditionOutput` to `FilterModels.cs`. Move `TitleRuleOutput` to `TitleRuleModels.cs`. Move `EnrichmentConfigInput`, `EnrichmentConfigOutput` and related types to `EnrichmentModels.cs`. Move `CreatedRuleSetResponse` to its own file. Move `FilterField`, `FilterOp`, `IdentificationStrategy`, `TitlePartType`, `EnrichmentMethod`, `RuntimeMode` enums. Update namespace.
- [x] 1.4 Create `Models/Scoring/` subfolder. Move `TestScoreRequest`, `TestCandidate` to `TestScoreRequest.cs`. Move `TestScoreResponse` to its own file. Move `ScoringDetail`, `ItemTrace`, `ScoreCandidate`, `RuleTrace`, `FilterGroupTrace`, `FilterNodeTrace`, `IdentificationTrace`, `TracedIdentification`, `EnrichmentTraceOutput` to `ScoringDetailResponse.cs`. Move `ScoringHistory`, `ScoringSnapshotSummary` to `ScoringHistoryResponse.cs`. Move `MatchMethod`, `RuleOutcome`, `SearchSource` enums. Update namespace.
- [x] 1.5 Create `Models/Feed/` subfolder. Move `FeedOverviewResponse`, `FeedEntryOutput`, `FeedCrawlStatusOutput` to `FeedOverviewResponse.cs`. Update namespace.
- [x] 1.6 Create `Models/Mediathek/` subfolder. Move `MediathekSearchRequest` and `MediathekSearchResponse`, `MediathekSearchResult` to their own files. Update namespace.
- [x] 1.7 Create `Models/System/` subfolder. Move `SetupHealthCheck`, `CheckResult`, `CheckStatus`, `SetupConnectionInfo` to `SetupHealthCheck.cs`. Move `VersionResponse`, `StorageStatusResponse`, `StorageDirectory`, `CacheStatsResponse`, `RoutesResponse`, `RouteDefinitionResponse`, `ChannelRouteResponse`, `VariantsResponse`, `VariantPolicyEntry` to their own files. Update namespace.
- [x] 1.8 Create `Models/Setup/` subfolder. Move `CreateArrResourceRequest`, `ArrPayloads` (all Arr payload records). Update namespace.
- [x] 1.9 Delete `Models/Enums.cs`. Update all `using` statements across endpoint files, mapping extensions, and test projects. Build and verify.

## 2. Enum-over-string changes

- [x] 2.1 Add `FilterOperator` enum (`All`, `Any`, `Not`) to `Models/Scoring/`. Change `FilterGroupTrace.Operator` from `string` to `FilterOperator`. Update `ScoringMappingExtensions` to map via exhaustive switch instead of `.ToString()`.
- [x] 2.2 Change `FilterNodeTrace.Field` from `string?` to `FilterField?` and `FilterNodeTrace.Op` from `string?` to `FilterOp?`. Update `ScoringMappingExtensions` to pass enum values instead of strings.
- [x] 2.3 Change `IdentificationTrace.Strategy` from `string?` to `IdentificationStrategy?`. Update `ScoringMappingExtensions` to map via exhaustive switch instead of `.ToString()`.
- [x] 2.4 Change `TestScoreRequest.MediaType` from `string?` to `MediaType?`. Update `TestScoreMappingExtensions` and `RuleSetApiEndpoints` to use the enum directly.
- [x] 2.5 Change `MediaInput.Type` from `string` to `MediaType`. Remove `ParseMediaType` from `RuleSetMappingExtensions`. Update `ToBody` to use the enum directly.
- [x] 2.6 Change `DownloadHistoryRequest.Category` from `string?` to `MediaType?`. Remove `ParseMediaType` from `DownloadsApiEndpoints`. Pass enum directly to `QueryHistory`.
- [x] 2.7 Change feed GET `type` query parameter from `string?` to `MediaType?`. Remove `ParseMediaType` from `FeedApiEndpoints`.
- [x] 2.8 Change `CacheStatsResponse.OldestEntry` from `string?` to `DateTimeOffset?`. Remove `.ToString("o")` from `SystemApiEndpoints`.
- [x] 2.9 Change `DownloadTimeSlotResponse.Start`/`End` from `string` to `TimeOnly`. Update `DownloadsApiEndpoints` settings handler to pass `TimeOnly` values directly.
- [x] 2.10 Configure `JsonStringEnumConverter` with camelCase naming on the API's `JsonSerializerOptions` so all enums serialize as camelCase strings. Update existing specs/tests that expect integer enum serialization.

## 3. Route path fixes

- [x] 3.1 Move `POST /{id}/retry` to `POST /queue/{id}/retry` in `DownloadsApiEndpoints`.
- [x] 3.2 Move `POST /pause` to `POST /queue/pause` and `POST /resume` to `POST /queue/resume` in `DownloadsApiEndpoints`.
- [x] 3.3 Change `MapGet("")` to `MapGet("/")` in `FeedApiEndpoints`.
- [x] 3.4 Update frontend API calls for retry (`/api/downloads/queue/{id}/retry`), pause (`/api/downloads/queue/pause`), resume (`/api/downloads/queue/resume`).
- [x] 3.5 Update `FunkArr.Api.Tests` for changed route paths.

## 4. Inline lambdas — RuleSetApiEndpoints

- [x] 4.1 Inline `HandleCreate` into the `MapPost("/", ...)` lambda. Inline `EvictRuleSetCache` call.
- [x] 4.2 Inline `HandleUpdate` into the `MapPut("/{id}", ...)` lambda. Inline `EvictRuleSetCache` call.
- [x] 4.3 Inline `HandleDelete` into the `MapDelete("/{id}", ...)` lambda. Inline `EvictRuleSetCache` call.
- [x] 4.4 Inline `HandleGetRaw` into the `MapGet("/{id}/raw", ...)` lambda.
- [x] 4.5 Inline `HandleExport` and `ExportResult` into the `MapGet("/{id}/export", ...)` lambda.
- [x] 4.6 Remove all `private static` handler methods from `RuleSetApiEndpoints`. Verify no method group references remain.

## 5. Move test enrichment to domain layer

- [x] 5.1 Create `TestScoreWithEnrichment` command and response types in `FunkArr.Messages.Scoring`. The command includes the scoring config, candidates, and optional enrichment parameters (TvdbId, TmdbId, ImdbId, enrichment config).
- [x] 5.2 Add handler for `TestScoreWithEnrichment` in `ScoringManager` (or a new dedicated actor). The handler orchestrates: score items → if enrichment enabled, forward matched items to `EnrichmentManager` → merge results → respond.
- [x] 5.3 Update `/api/rulesets/test` endpoint to send `TestScoreWithEnrichment` via single Ask. Remove `RunTestEnrichment`, `EnrichEpisodes`, `EnrichMovies` from `RuleSetApiEndpoints`.
- [x] 5.4 Add/update tests for the test-score-with-enrichment flow.

## 6. Inline lambdas — remaining endpoint files

- [x] 6.1 Inline `ResolveSelfUrl` into each lambda in `SetupArrEndpoints`.
- [x] 6.2 Remove `ParseMediaType` from `DownloadsApiEndpoints` (already dead after 2.6). Remove `ParseMediaType` from `FeedApiEndpoints` (already dead after 2.7).
- [x] 6.3 Move `ToApi(FeedResult, FeedCrawlStatusResult)` and `FeedEntry.MediaType.ToApi()` from `FeedApiEndpoints` to a new `Extensions/FeedMappingExtensions.cs`.
- [x] 6.4 Move `ToVariantsResponse` and `ToApi(VariantPolicy)` from `SystemApiEndpoints` to a new `Extensions/SystemMappingExtensions.cs`.
- [x] 6.5 Verify all endpoint files: no `private static` handler methods remain (except SystemApiEndpoints health probes).

## 7. AGENTS.md and documentation

- [x] 7.1 Add "API conventions (FunkArr.Api)" section to AGENTS.md covering: inline-only lambdas, endpoint lambda shape, route path rules, model organization, enums-over-strings, mapping extension placement, SystemApiEndpoints exception.

## 8. Build and verify

- [x] 8.1 `dotnet build src/FunkArr.slnx` — zero errors, zero warnings.
- [x] 8.2 `dotnet format src/FunkArr.slnx --verify-no-changes` — clean.
- [x] 8.3 Run all test projects — all green.
- [x] 8.4 Verify frontend builds and basic smoke test (queue page, ruleset list, test scoring).
