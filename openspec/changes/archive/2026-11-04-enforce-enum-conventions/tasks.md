## 1. Persistence — fix partial explicit values

- [x] 1.1 Fix `PersistedSearchSource`: add explicit values `Sonarr = 0, Radarr = 1, Prowlarr = 2, Feed = 4`

## 2. Messages — explicit ints and Unknown = 0

- [x] 2.1 Add explicit ints to `FailureKind`, `WorkerStatus` (no Unknown — exempt or small)
- [x] 2.2 Add `Unknown = 0` + explicit ints to `MediathekVariant`, `MediaType`, `SearchSource`
- [x] 2.3 Add `Unknown = 0` + explicit ints to `EnrichmentMethod`, `MatchMethod`, `RuntimeMode`
- [x] 2.4 Add `Unknown = 0` + explicit ints to `FilterField`, `FilterGroupOp`, `FilterOp`, `TitlePartType`
- [x] 2.5 Add `Unknown = 0` + explicit ints to `IdentificationStrategy`, `IdentificationFailureReason`, `RuleOutcome`
- [x] 2.6 Add explicit ints to FailureReason enums in `LocalRuleSetCommands.cs` (no Unknown — exempt)
- [x] 2.7 Verify `DownloadPhase`, `DownloadPriority`, `DownloadStatus` remain unchanged (already compliant)

## 3. Api — explicit ints, Unknown = 0, and SourceType fix

- [x] 3.1 Fix `SourceType`: reposition `Unknown = 0` first, renumber `Community = 1, Local = 2, Merged = 3`
- [x] 3.2 Add `Unknown = 0` + explicit ints to `QueueStatus`, `HistoryStatus`, `QueuePhase`, `CheckStatus`
- [x] 3.3 Add `Unknown = 0` + explicit ints to `MediaType`, `MediathekVariant`, `VariantPolicy`
- [x] 3.4 Add `Unknown = 0` + explicit ints to `MatchMethod`, `RuleOutcome`, `SearchSource`, `FilterOperator`
- [x] 3.5 Add `Unknown = 0` + explicit ints to `TitlePartType`, `FilterField`, `FilterOp`
- [x] 3.6 Add `Unknown = 0` + explicit ints to `EnrichmentMethod`, `RuntimeMode`, `IdentificationStrategy`
- [x] 3.7 Add explicit ints to `QueuePriority` (no Unknown — exempt: `Low = -1, Normal = 0, High = 1`)

## 4. Domain projects — explicit ints

- [x] 4.1 Add `Unknown = 0` + explicit ints to `CrawlStatus` in FunkArr.Feed
- [x] 4.2 Add `Unknown = 0` + explicit ints to `VariantPolicy` in FunkArr.Core
- [x] 4.3 Add explicit ints to `SearchType` in SearchManager (no Unknown — actor-internal)
- [x] 4.4 Add explicit ints to `SubtitleFailureReason` in FunkArr.Download (no Unknown — internal)

## 5. Mapping extensions — handle shifted values

- [x] 5.1 Update `EnumMappingExtensions` for Messages↔Api enum mappings with shifted member values — no changes needed, maps by name
- [x] 5.2 Update `DownloadMappingExtensions` for any affected download enum mappings — no changes needed
- [x] 5.3 Update `RuleSetMappingExtensions` for affected ruleset enum mappings — no changes needed
- [x] 5.4 Update `ScoringMappingExtensions` for affected scoring enum mappings — no changes needed
- [x] 5.5 Update `FeedMappingExtensions` for affected feed enum mappings — no changes needed
- [x] 5.6 Update `SystemMappingExtensions` for `CheckStatus` mapping — no changes needed
- [x] 5.7 Update `MediathekMappingExtensions` for `MediathekVariant` mapping — no changes needed
- [x] 5.8 Update `TestScoreMappingExtensions` for affected test score mappings — no changes needed
- [x] 5.9 Update domain-project mapping extensions (History, etc.) if any enum values shifted — no changes needed

## 6. Tests — update for new enum values

- [x] 6.1 Update `EnumMappingExtensionsTests` for new `Unknown` members and shifted values
- [x] 6.2 Update `DownloadMappingExtensionsTests` for affected mappings — no changes needed
- [x] 6.3 Update `RuleSetMappingExtensionsTests` for affected mappings — no changes needed
- [x] 6.4 Update `ScoringMappingExtensionsTests` for affected mappings — no changes needed
- [x] 6.5 Update `TestScoreMappingExtensionsTests` for affected mappings — no changes needed
- [x] 6.6 Update History `EnumMappingTests` + `PersistenceMappingTests` for Unknown filtering

## 7. Verify

- [x] 7.1 `dotnet build src/FunkArr.slnx` — zero warnings
- [x] 7.2 `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 7.3 Run all test projects and verify green (1 pre-existing unrelated failure in EndpointExceptionFilterTests)
