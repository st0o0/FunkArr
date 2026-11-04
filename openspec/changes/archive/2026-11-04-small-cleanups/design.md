## Context

Several small code hygiene issues have accumulated across the codebase. Each is individually trivial but together they represent inconsistent patterns, misplaced types, and missing validation. All changes are internal refactors with no API or persistence impact.

## Goals / Non-Goals

**Goals:**
- Consistent extension method patterns where `this T` is the natural receiver
- Standardized PersistenceId pattern for entity actors, documented as convention
- Correct domain placement for types (implementation in owning domain, not Core)
- Every Options class validated on startup via `AddOptionsWithValidateOnStart<T, V>()`
- No manual `new Options()` + `Bind()` bypassing DI

**Non-Goals:**
- Adding ArchUnit tests for Options/Validator coverage (separate concern)
- Refactoring RoutingOptions sub-types out of Core (they're part of config binding)
- Changing any external API behavior or persistence format

## Decisions

### TimeOnly Extensions
Convert `DownloadScheduleHelper` static class to `TimeOnlyExtensions`. Methods like `IsWithinSchedule(TimeOnly now, ...)` become `now.IsWithinSchedule(...)`. Private helpers (`IsWithinSlot`, `DelayUntil`) become `internal` extension methods (tests already cover them indirectly). File stays in `FunkArr.Download`.

### PersistenceId field-pattern
Use C# 14 `field` keyword for entity actor PersistenceId:
```csharp
public override string PersistenceId => $"prefix-{field}";
```
Constructor receives the raw entity ID and assigns to `field` via the backing field. This is cleaner than the `{ get; }` auto-prop pattern because the prefix is visible in the getter. Singletons keep their literal `=> "name"` pattern — no change needed there.

### ProcessCommand extraction
`HistoryState.ProcessCommand(RecordHistory)` maps a command to a persistence DTO. The same mapping is duplicated in `GetPersistenceState()`. Extract to `RecordHistory.ToPersistence()` extension in a `HistoryMappingExtensions.cs` file in `FunkArr.History`. Both call sites use the extension. `ProcessCommand` method is removed from the state class.

### RouteResolver relocation
Move `RouteResolver.cs` from `FunkArr.Core` to `FunkArr.Download`. The interface `IRouteResolver` and return type `ResolvedRoute` stay in Core (they're the cross-domain contract). Only `Remuxer` and `DownloadServiceExtensions` consume the implementation. DI registration moves from `CoreSetupContainer` to `DownloadSetupContainer`.

### IRuleSetValidator relocation
Move `IRuleSetValidator.cs` from `FunkArr.Core` to `FunkArr.RuleSet`. Both the implementation (`RuleSetValidator`) and sole consumer (`RuleSetWorker`) are in `FunkArr.RuleSet`. No other domain references it.

### Manual RoutingOptions binding fix
`DownloadServiceExtensions` currently does `new RoutingOptions()` + `configuration.Bind()` to read route definitions at DI registration time for named HttpClient setup. Replace with injecting `IOptions<RoutingOptions>` via an `IConfigureNamedOptions` implementation or by restructuring the HttpClient registration to use `IOptionsMonitor` at runtime instead of at registration time.

**Alternative considered:** Post-configure pattern — rejected because the HttpClient names need to be known at registration time. The `IConfigureNamedOptions` approach or passing `IConfiguration` directly (which is already available) and reading the section is simpler than the manual bind.

### Missing validators
Create `IValidateOptions<T>` implementations for `VariantOptions`, `TvdbOptions`, `TmdbOptions`. Each validates required fields (API keys, base URLs, etc.) are not empty. Update registrations from `AddOptionsWithValidateOnStart<T>()` to `AddOptionsWithValidateOnStart<T, V>()`.

## Risks / Trade-offs

- **C# 14 `field` keyword** — requires .NET 10 / C# 14. Project already targets net10.0 so this is fine. Older tooling may not understand the syntax → not a concern since CI uses current SDK.
- **RouteResolver move** — DI registration moves from Core to Download setup container. If another domain ever needs route resolution, the interface is still in Core. Low risk.
- **IRuleSetValidator move** — if another domain ever needs ruleset validation, the interface would need to move back. Extremely unlikely given it validates domain-specific JSON schemas.
