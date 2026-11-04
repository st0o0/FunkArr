## 1. Preparation

- [x] 1.1 Commit pending working-tree changes (done together with this change)
- [x] 1.2 Record the baseline warning count (`dotnet build src/FunkArr.slnx` with `NoWarn` overridden): ~529 xUnit1051, 1 CS8604

## 2. Persistence enum values

- [x] 2.1 Add explicit values (current ordinals from 0) to `PersistedMediaType`, `PersistedFailureKind`, `PersistedMatchMethod`, `PersistedMediathekVariant`, `PersistedRuleOutcome`
- [x] 2.2 Add explicit values to `PersistedFilterGroupOp`, `PersistedIdentificationStrategy`, `PersistedIdentificationFailureReason`
- [x] 2.3 Confirm `PersistedDownloadStatus`, `PersistedDownloadPriority`, `PersistedSearchSource` are unchanged
- [x] 2.4 Run `FunkArr.Persistence.Tests` to confirm Verify snapshots are unchanged

## 3. Switch-based enum mapping

- [x] 3.1 Convert the 12 casts in `FunkArr.History/PersistenceMapping.cs` to `ToPersistence()`/`ToDomain()` switch helpers with throwing default (including nullable enums)
- [x] 3.2 Convert all Api <-> Messages enum casts (~35: MediaType, SearchSource, MediathekVariant, FilterOp, FilterField, IdentificationStrategy, TitlePartType, EnrichmentMethod, RuntimeMode, MatchMethod, QueuePriority) via central `EnumMappingExtensions` in FunkArr.Api
- [x] 3.3 Convert `DownloadPriority` casts in `FunkArr.Api/DownloadsApiEndpoints.cs`, plus Feed/RuleSet/Download endpoint casts
- [x] 3.4 Convert the priority int cast in `FunkArr.ArrApi/Sabnzbd/SabnzbdDownloadService.cs` (clamp, then switch)
- [x] 3.5 Add tests: round trip of every member (History, Api), throwing default for undefined values, frozen persistence enum values (Sabnzbd priority switch has no dedicated test)
- [x] 3.6 Run History, Api, ArrApi test projects

## 4. CS8604 fix

- [x] 4.1 Fix the null `FailMessage` argument in `FunkArr.ArrApi/Sabnzbd/SabnzbdResponseMapper.cs`
- [x] 4.2 Add a test for a history slot without a failure message

## 5. xUnit1051 fixes

- [x] 5.1 Fix `FunkArr.IntegrationTests` (largest share)
- [x] 5.2 Fix `FunkArr.Search.Tests` and `FunkArr.Api.Tests`
- [x] 5.3 Fix `FunkArr.Download.Tests`, `FunkArr.RuleSet.Tests`
- [x] 5.4 Fix `FunkArr.History.Tests`, `FunkArr.Feed.Tests`, `FunkArr.ArrApi.Tests`, `FunkArr.Scoring.Tests`, `FunkArr.Enrichment.Tests`
- [x] 5.5 Verify zero xUnit1051 remain with `NoWarn` overridden on the command line

## 6. Enforce warnings as errors

- [x] 6.1 Remove `xUnit1051` from `NoWarn` in `src/Directory.Build.props`
- [x] 6.2 Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to `src/Directory.Build.props`
- [x] 6.3 `dotnet build src/FunkArr.slnx` completes with 0 warnings and 0 errors
- [x] 6.4 `dotnet format src/FunkArr.slnx --verify-no-changes` reports no violations except ENDOFLINE on Windows (autocrlf working copy, also in untouched files)

## 7. Documentation

- [x] 7.1 `AGENTS.md`: add persistence enum rule (explicit frozen int values, never reorder or reuse)
- [x] 7.2 `AGENTS.md`: add switch-only enum mapping rule with throwing default
- [x] 7.3 `AGENTS.md`: note warnings are errors and tests pass `TestContext.Current.CancellationToken`
- [x] 7.4 Update `funkarr-message` skill with the persistence enum and mapping rules

## 8. Final verification

- [x] 8.1 Run all test projects via `dotnet run --project ...`
- [x] 8.2 Run `openspec validate enum-conventions-warnings-as-errors`
