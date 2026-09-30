## 1. Persistence enum isolation

- [x] 1.1 Create `PersistedMediaType` enum in `FunkArr.Persistence/` with identical integer values to `Messages.MediaType`
- [x] 1.2 Create `PersistedSearchSource` enum in `FunkArr.Persistence/` with identical integer values to `Messages.SearchSource`
- [x] 1.3 Create `PersistedRuleOutcome` enum in `FunkArr.Persistence/` with identical integer values to `Messages.Scoring.History.RuleOutcome`
- [x] 1.4 Update `DownloadInitialized` and `Download.HistoryRecorded` to use `PersistedMediaType`
- [x] 1.5 Update `ScoringHistory.HistoryRecorded` to use `PersistedSearchSource`

## 2. Persistence ItemTrace tree isolation

- [x] 2.1 Create `PersistedItemTrace` record in `FunkArr.Persistence/Events/ScoringHistory/`
- [x] 2.2 Create `PersistedRuleTrace` record
- [x] 2.3 Create `PersistedFilterGroupTrace` record
- [x] 2.4 Create `PersistedFilterNodeTrace` record
- [x] 2.5 Create `PersistedTracedIdentification` record
- [x] 2.6 Create `PersistedEnrichmentTrace` record
- [x] 2.7 Update `ScoringHistory.HistoryRecorded` to use `PersistedItemTrace[]` instead of `Messages.ItemTrace[]`
- [x] 2.8 Update `PersistedHistoryState` to use the new Persisted types

## 3. Persistence mapping and decoupling

- [x] 3.1 Create mapping extensions `ToPersistence()` / `FromPersistence()` for ItemTrace tree (in domain project that persists — FunkArr.History)
- [x] 3.2 Create mapping extensions for enums (`MediaType` ↔ `PersistedMediaType`, `SearchSource` ↔ `PersistedSearchSource`)
- [x] 3.3 Update `HistoryWorker` to use mapping when persisting and recovering
- [x] 3.4 Update Download domain actors to use `PersistedMediaType` mapping when persisting
- [x] 3.5 Remove ProjectReference from `FunkArr.Persistence.csproj` → `FunkArr.Messages`
- [x] 3.6 Build and fix any remaining compile errors

## 4. Api enum isolation

- [x] 4.1 Create Api-owned `MediaType` enum in `FunkArr.Api/Models/` (plain, int serialization)
- [x] 4.2 Create Api-owned `SearchSource` enum
- [x] 4.3 Create Api-owned `FilterOp` enum
- [x] 4.4 Create Api-owned `FilterField` enum
- [x] 4.5 Create Api-owned `IdentificationStrategy` enum
- [x] 4.6 Create Api-owned `TitlePartType` enum
- [x] 4.7 Create Api-owned `EnrichmentMethod` enum
- [x] 4.8 Create Api-owned `RuntimeMode` enum
- [x] 4.9 Create Api-owned `MatchMethod` enum
- [x] 4.10 Update all Api.Models records to use Api-owned enums instead of Messages enums

## 5. Api complex type isolation

- [x] 5.1 Create Api-owned copy of `FilterGroupOutput` in `FunkArr.Api/Models/`
- [x] 5.2 Create Api-owned copy of `TitleRuleOutput` in `FunkArr.Api/Models/`
- [x] 5.3 Update `RuleSetDetailRule` and related types to use Api-owned copies
- [x] 5.4 Add/update `ToApi()` mapping extensions for the new types

## 6. Api mapping consistency

- [x] 6.1 Add `ToApi()` / `FromApi()` mapping methods for all 9 new Api enums
- [x] 6.2 Replace any inline field-by-field construction in endpoints with `.ToApi()` calls
- [x] 6.3 Verify no Messages types are used directly as field types in any Api.Models record

## 7. Messages enum cleanup

- [x] 7.1 Remove `[JsonStringEnumConverter]` and `[JsonStringEnumMemberName]` attributes from all Messages enums
- [x] 7.2 Build and fix any compile errors from attribute removal (RuleSetMerger fixed with own JsonSerializerOptions)

## 8. State type isolation

- [x] 8.1 ~Create internal `ScoringConfig` record~ — Skipped: state records are pass-through in-memory caches, no serialization concern
- [x] 8.2 ~Add `FromMessage()` / `ToMessage()` mapping~ — Skipped
- [x] 8.3 ~Refactor `StatsCollectorState`~ — Skipped
- [x] 8.4 ~Refactor `RuleSetResolverState`~ — Skipped

## 9. Verify

- [x] 9.1 Run `dotnet build src/FunkArr.slnx` — zero errors
- [x] 9.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes` — passes (pre-existing issues in unrelated files only)
- [x] 9.3 Run all test projects — all 558 pass
- [x] 9.4 Verify `FunkArr.Persistence.csproj` has no ProjectReference to FunkArr.Messages
- [x] 9.5 Grep: no `FunkArr.Messages` using statements in `FunkArr.Persistence/` source files
- [x] 9.6 Grep: no `Messages.*` enum types used as field types in `FunkArr.Api/Models/`
