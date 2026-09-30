## Tasks

- [x] Add FilterGroupOp enum - Create `FilterGroupOp` enum with members `All`, `Any`, `Not` in `FunkArr.Messages.Scoring` namespace. File: `src/FunkArr.Messages/Scoring/FilterGroupOp.cs`
- [x] Add IdentificationFailureReason enum - Create enum with members: `UnknownStrategy`, `SeasonPatternNotMatched`, `NoEpisodePatternConfigured`, `EpisodePatternNotMatched`, `NoTitlePartsConfigured`, `TitlePartRegexNotMatched`, `TitleDoesNotMatch`, `NoDateFoundInTitle`. File: `src/FunkArr.Messages/Scoring/History/IdentificationFailureReason.cs`
- [x] Update FilterGroupTrace record - Change `string Operator` to `FilterGroupOp Operator`. File: `src/FunkArr.Messages/Scoring/History/FilterGroupTrace.cs`
- [x] Update IdentificationTrace record - Change `string? Strategy` to `IdentificationStrategy? Strategy`, `string? Detail` to `IdentificationFailureReason? Detail`. File: `src/FunkArr.Messages/Scoring/History/IdentificationTrace.cs`
- [x] Update ScoringEngine - Change `EvaluateGroupTraced` param from `string op` to `FilterGroupOp op`, switch on enum values. Pass `FilterGroupOp.All/.Any/.Not` in `EvaluateFiltersTraced`. Use `spec.Strategy` directly, `IdentificationStrategy.AirdateExtraction` enum, and `IdentificationFailureReason` values in all identification methods. File: `src/FunkArr.Scoring/ScoringEngine.cs`
- [x] Update PersistenceMapping - Convert enum<->string in `ToPersistence()`/`ToDomain()` for FilterGroupTrace and IdentificationTrace. Persistence DTOs stay string-typed. File: `src/FunkArr.History/PersistenceMapping.cs`
- [x] Update API mapping - Convert enum->string in `ToApi()` for FilterGroupTrace and IdentificationTrace. API models stay string-typed. File: `src/FunkArr.Api/Extensions/ScoringMappingExtensions.cs`
- [x] Update tests - Update assertions and builders to use enum values instead of string literals. Files: `src/FunkArr.Scoring.Tests/*.cs`, `src/FunkArr.Tests.Shared/TestItemTraceBuilder.cs`
- [x] Build and verify - Run `dotnet build`, `dotnet format --verify-no-changes`, and scoring tests. Fix any failures.
