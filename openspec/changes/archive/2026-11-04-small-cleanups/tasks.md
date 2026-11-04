## 1. Extension Method Refactors

- [x] 1.1 Convert `DownloadScheduleHelper` to `TimeOnlyExtensions` in `FunkArr.Download` — rename class, add `this TimeOnly` to public methods, make private helpers internal extensions
- [x] 1.2 Update `DownloadScheduler` call sites to use extension syntax
- [x] 1.3 Update `DownloadScheduleHelperTests` to use extension syntax
- [x] 1.4 Extract `RecordHistory.ToPersistence()` and `HistoryEntry.ToPersistence()` extensions in `FunkArr.History/PersistenceMapping.cs`
- [x] 1.5 Replace `HistoryState.ProcessCommand` and duplicated mapping in `GetPersistenceState` with `ToPersistence()` extension call; update `HistoryWorker` to call extension directly

## 2. PersistenceId Standardization

- [x] 2.1 Convert `DownloadWorker` PersistenceId from `{ get; }` auto-prop to `=> $"download-{field}"` with constructor assigning backing field
- [x] 2.2 Document PersistenceId `field` keyword convention in AGENTS.md under Akka conventions

## 3. Core Type Relocation

- [x] 3.1 Move `RouteResolver.cs` from `FunkArr.Core` to `FunkArr.Download`
- [x] 3.2 Move `IRouteResolver` DI registration from `CoreSetupContainer` to `DownloadSetupContainer`
- [x] 3.3 Move `IRuleSetValidator.cs` from `FunkArr.Core` to `FunkArr.RuleSet`

## 4. Options / Validator Hygiene

- [x] 4.1 Replace manual `new RoutingOptions()` + `Bind()` in `DownloadServiceExtensions` with `configuration.GetSection().Get<T>()`
- [x] 4.2 Create `VariantOptionsValidator` in `FunkArr.Core/Configuration`, register in `ScoringSetupContainer` via `AddOptionsWithValidateOnStart<VariantOptions, VariantOptionsValidator>()`
- [x] 4.3 Create `TvdbOptionsValidator` in `FunkArr.Core/Configuration`, register in `EnrichmentSetupContainer`
- [x] 4.4 Create `TmdbOptionsValidator` in `FunkArr.Core/Configuration`, register in `EnrichmentSetupContainer`

## 5. Verification

- [x] 5.1 `dotnet build src/FunkArr.slnx` — zero errors, zero warnings
- [x] 5.2 `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 5.3 Run all affected test projects: Download.Tests, History.Tests, Scoring.Tests, Enrichment.Tests
