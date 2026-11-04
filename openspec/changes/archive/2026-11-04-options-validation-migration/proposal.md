## Why

Options registration across all setup containers uses a mix of patterns: manual
`.AddOptions<T>().Bind().ValidateOnStart()`, separate `AddSingleton<IValidateOptions<T>>`,
inline `.Validate()` lambdas, and three types that skip `ValidateOnStart` entirely.
Terp 0.2.1 ships `ValidationFailures` for cleaner validator bodies. Standardizing
on `AddOptionsWithValidateOnStart` (Microsoft API) and `ValidationFailures` (Terp)
gives one registration path and one validator pattern across all 12 options types.

## What Changes

- Bump Terp from 0.2.0 to 0.2.1 in `Directory.Packages.props`.
- Fix Feed `ObservableGauge` leftover from the Servus migration (→ `Gauge<int>`).
- Convert all 12 options registrations to `AddOptionsWithValidateOnStart<T>` (or
  `<T, TValidator>` where a validator exists).
- Remove separate `AddSingleton<IValidateOptions<T>>` lines for `RoutingOptions`
  and `DownloadOptions`.
- Extract `FeedOptionsValidator` from 8 inline `.Validate()` lambdas using
  `ValidationFailures("FunkArr:Feed")`.
- Convert `DownloadOptionsValidator` and `RoutingOptionsValidator` from
  `List<string>` to `ValidationFailures()`.
- Normalize `EnrichmentSetupContainer` from `BindConfiguration()` to
  `Bind(context.Configuration.GetSection(...))`.
- `PostgresOptions`, `TvdbOptions`, `TmdbOptions` gain `ValidateOnStart`
  (binding-error detection at startup; no validators added).
- Delete `Migration.md` (fully executed after this change).

## Capabilities

### New Capabilities

- `options-registration`: Standardized options registration pattern using
  `AddOptionsWithValidateOnStart` and `ValidationFailures` across all setup
  containers.

### Modified Capabilities

- `setup-container-layout`: Registration calls change shape in every setup
  container (mechanical, same behavior).
- `download-options`: `DownloadOptionsValidator` switches to `ValidationFailures`.

## Impact

- **Files**: 7 setup containers, 1 domain extension method
  (`ArrApi/ServiceCollectionExtensions.cs`), 2 existing validators, 1 new
  validator + tests, `Feed/Telemetry.cs`, `Directory.Packages.props`,
  `Migration.md` (deleted).
- **Dependencies**: Terp 0.2.0 → 0.2.1 (Terp.Akka stays 0.2.0).
- **Behavior**: `PostgresOptions`, `TvdbOptions`, `TmdbOptions` now validated
  on start (binding errors surface at startup instead of first use). No other
  runtime behavior changes.
- **Metrics**: Feed `entries` gauge changes from `ObservableGauge` to `Gauge<int>`
  (same metric name, same semantics).
