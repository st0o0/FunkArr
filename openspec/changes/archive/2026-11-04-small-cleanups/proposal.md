## Why

Several small inconsistencies and hygiene issues have accumulated: a static helper that should be extensions, inconsistent PersistenceId patterns across entity actors, a duplicated message-to-persistence mapping, a domain implementation misplaced in Core, an unused cross-domain interface, a manual Options binding bypassing DI, and three Options classes without validators.

## What Changes

- Convert `DownloadScheduleHelper` to `TimeOnly` extension methods
- Standardize entity actor PersistenceId on C# 14 `field` keyword pattern, document in AGENTS.md
- Extract `HistoryState.ProcessCommand` to `RecordHistory.ToPersistence()` extension, remove duplication with `GetPersistenceState()`
- Move `RouteResolver` implementation from Core to FunkArr.Download (interface stays in Core)
- Move `IRuleSetValidator` from Core to FunkArr.RuleSet (no cross-domain consumers)
- Replace manual `new RoutingOptions()` + `Bind()` in `DownloadServiceExtensions` with proper DI
- Create validators for `VariantOptions`, `TvdbOptions`, `TmdbOptions` and register them

## Capabilities

### New Capabilities

- `options-validation-hygiene`: Ensure every Options class has a validator registered via `AddOptionsWithValidateOnStart<T, V>()`; eliminate manual Options binding patterns
- `extension-method-refactors`: Convert static helpers and duplicated mappings to idiomatic extension methods
- `core-type-placement`: Relocate types that violate domain isolation (implementation in Core, unused cross-domain interfaces)
- `persistence-id-convention`: Standardize entity actor PersistenceId pattern using C# 14 `field` keyword

### Modified Capabilities

## Impact

- **FunkArr.Download**: Gains `RouteResolver.cs`, `TimeOnlyExtensions.cs`; loses `DownloadScheduleHelper.cs`; `DownloadServiceExtensions.cs` refactored
- **FunkArr.Core**: Loses `RouteResolver.cs`, `IRuleSetValidator.cs`
- **FunkArr.RuleSet**: Gains `IRuleSetValidator.cs`
- **FunkArr.History**: Gains `RecordHistoryMappingExtensions.cs`; `HistoryState.cs` simplified
- **FunkArr.Scoring**: `ScoringSetupContainer` updated for `VariantOptionsValidator`
- **FunkArr.Enrichment**: `EnrichmentSetupContainer` updated for `TvdbOptionsValidator`, `TmdbOptionsValidator`
- **AGENTS.md**: New convention for entity actor PersistenceId pattern
- No API changes, no persistence changes, no breaking changes
