## Context

FunkArr registers 12 options types across 7 setup containers and 1 domain
extension method. The current code uses three different patterns:

1. `.AddOptions<T>().Bind(...).ValidateOnStart()` — most types
2. Same + separate `AddSingleton<IValidateOptions<T>>` — Routing, Download
3. `.AddOptions<T>().Bind(...)` + 8 inline `.Validate()` lambdas — Feed

Three types (`PostgresOptions`, `TvdbOptions`, `TmdbOptions`) skip
`ValidateOnStart` entirely. `EnrichmentSetupContainer` uses
`BindConfiguration()` instead of `Bind(context.Configuration.GetSection(...))`
and ignores its `SetupContext`.

Terp 0.2.1 ships `Terp.Options.ValidationFailures` — a collector that replaces
the `List<string>` + ternary pattern in validators. `AddOptionsWithValidateOnStart`
is standard Microsoft API (`Microsoft.Extensions.Options`), not Terp.

## Goals / Non-Goals

**Goals:**

- One registration call (`AddOptionsWithValidateOnStart`) for all options types.
- One validator body pattern (`ValidationFailures`) for all validators.
- All options validated on startup (binding errors caught early).
- Consistent `Bind(context.Configuration.GetSection(...))` everywhere.
- Fix the Feed `ObservableGauge` leftover from the Servus migration.
- Delete `Migration.md` (fully executed).

**Non-Goals:**

- Adding validators for types that don't have them (Postgres, Tvdb, Tmdb,
  FunkArr, Scoring*, RuleSetUpdater, ArrApi). Just `ValidateOnStart` for
  binding-error detection.
- Changing validation rules or messages (FeedOptionsValidator replicates the
  inline rules exactly).
- Migrating `BindConfiguration` to `Bind` in ArrApi's `ServiceCollectionExtensions`
  (it already uses `Bind`).

## Decisions

### Use `AddOptionsWithValidateOnStart<T>` (no validator) for simple types

Types without a validator class get `AddOptionsWithValidateOnStart<T>().Bind(...)`.
This is equivalent to `.AddOptions<T>().Bind(...).ValidateOnStart()` but in one
call that communicates intent: "this type participates in startup validation."

The Microsoft API registers the `IValidateOptions` infrastructure internally.
For types without a custom validator, only binding errors are caught (e.g. a
non-numeric string bound to an `int` property).

### Use `AddOptionsWithValidateOnStart<T, TValidator>` for validated types

Types with a validator class get the two-generic overload. This replaces the
separate `AddSingleton<IValidateOptions<T>, TValidator>()` call — the Microsoft
API does the registration internally.

Applies to: `RoutingOptions`, `DownloadOptions`, `FeedOptions` (new validator).

### `ValidationFailures` without section for existing validators

`DownloadOptionsValidator` and `RoutingOptionsValidator` currently produce
messages without a section prefix (e.g. `"DownloadSchedule[0]: Start and End
must differ"`). Using `new ValidationFailures()` (no section argument) preserves
these messages exactly. Test assertions remain unchanged.

### `ValidationFailures("FunkArr:Feed")` for FeedOptionsValidator

The inline `.Validate()` messages currently include the section prefix
(e.g. `"FunkArr:Feed:Interval must be positive"`). `ValidationFailures`
with a section argument produces `"{section}:{message}"`, so using
`new ValidationFailures(FeedOptions.SectionName)` with short messages
(`"Interval must be positive"`) reproduces the same output.

### Normalize EnrichmentSetupContainer

Change `BindConfiguration(SectionName)` to
`Bind(context.Configuration.GetSection(SectionName))` and rename the
`SetupContext _` parameter to `context`. This aligns with every other container.

### Feed Gauge fix

`Feed/Telemetry.cs` still uses `ObservableGauge` with a backing field and setter.
Download and RuleSet already use `Gauge<int>`. Convert to the same pattern:
`Gauge<int>` field, callers use `.Record(value)` instead of `SetEntries(int)`.

## Risks / Trade-offs

- **PostgresOptions/TvdbOptions/TmdbOptions now validated on startup** →
  If binding fails (e.g. wrong type in config), the host crashes at start
  instead of at first use. This is intentional — fail-fast is better — but
  could surprise if someone had a broken config that "worked" because the
  code path was never hit. Low risk: these are simple POCOs with primitive
  properties and sane defaults.

- **Feed gauge type change** → `ObservableGauge` polls via callback;
  `Gauge<int>` is push-based via `.Record()`. Functionally identical for a
  value set by a single actor. The metric name and semantics are unchanged.
  No dashboard impact.
