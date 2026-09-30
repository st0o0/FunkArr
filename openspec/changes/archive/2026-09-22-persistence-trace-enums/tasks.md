## Tasks

- [x] Add PersistedFilterGroupOp enum - Create enum in `FunkArr.Persistence/Events/ScoringHistory/` with members `All`, `Any`, `Not` matching `FilterGroupOp` order
- [x] Add PersistedIdentificationStrategy enum - Create enum in `FunkArr.Persistence/Events/ScoringHistory/` with members `SeasonAndEpisodeNumber`, `AbsoluteEpisodeNumber`, `TitleExact`, `TitleIncludes`, `AirdateExtraction` matching `IdentificationStrategy` order
- [x] Add PersistedIdentificationFailureReason enum - Create enum in `FunkArr.Persistence/Events/ScoringHistory/` with members matching `IdentificationFailureReason` order
- [x] Update PersistedFilterGroupTrace - Change `string Operator` to `PersistedFilterGroupOp Operator`
- [x] Update PersistedIdentificationTrace - Change `string? Strategy` to `PersistedIdentificationStrategy? Strategy`, `string? Detail` to `PersistedIdentificationFailureReason? Detail`
- [x] Update PersistenceMapping - Replace `Enum.Parse<>()` / `.ToString()` with `(int)` cast for FilterGroupTrace and IdentificationTrace mappings
- [x] Update TestItemTraceBuilder - Use new persisted enum values instead of string literals
- [x] Build and verify - Run `dotnet build`, `dotnet format --verify-no-changes` (changed files only), and scoring tests
