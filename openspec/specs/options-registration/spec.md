# Options Registration

## Purpose

Defines the standardized options registration pattern using
`AddOptionsWithValidateOnStart` and `Terp.Options.ValidationFailures`
across all setup containers.

## ADDED Requirements

### Requirement: Options registration via AddOptionsWithValidateOnStart

Every options type SHALL be registered using `AddOptionsWithValidateOnStart<T>()`
(without validator) or `AddOptionsWithValidateOnStart<T, TValidator>()` (with
validator), followed by `.Bind(context.Configuration.GetSection(T.SectionName))`.

The following types use the single-generic form (no custom validator):
`FunkArrOptions`, `PostgresOptions`, `ScoringOptions`, `VariantOptions`,
`ScoringHistoryOptions`, `RuleSetUpdaterOptions`, `TvdbOptions`, `TmdbOptions`,
`ArrApiOptions`.

The following types use the two-generic form (custom validator):
`RoutingOptions` with `RoutingOptionsValidator`,
`DownloadOptions` with `DownloadOptionsValidator`,
`FeedOptions` with `FeedOptionsValidator`.

#### Scenario: Type without validator

- **WHEN** an options type has no custom validator (e.g. `ScoringOptions`)
- **THEN** it SHALL be registered as `services.AddOptionsWithValidateOnStart<ScoringOptions>().Bind(context.Configuration.GetSection(ScoringOptions.SectionName))`
- **AND** binding errors SHALL be detected at startup

#### Scenario: Type with validator

- **WHEN** an options type has a custom validator (e.g. `DownloadOptions`)
- **THEN** it SHALL be registered as `services.AddOptionsWithValidateOnStart<DownloadOptions, DownloadOptionsValidator>().Bind(context.Configuration.GetSection(DownloadOptions.SectionName))`
- **AND** no separate `AddSingleton<IValidateOptions<T>>` call SHALL exist

#### Scenario: All options validated on start

- **WHEN** the host starts
- **THEN** all 12 options types SHALL be validated on startup
- **AND** a binding error in any type SHALL prevent the host from starting

### Requirement: Validators use ValidationFailures

Validator classes implementing `IValidateOptions<T>` SHALL use
`Terp.Options.ValidationFailures` instead of `List<string>` to collect
failures. The validator body SHALL call `failures.Require(condition, message)`
or `failures.Add(message)` and return `failures.ToResult()`.

#### Scenario: Validator with section prefix

- **WHEN** a validator needs messages prefixed with the config section (e.g. FeedOptionsValidator)
- **THEN** it SHALL use `new ValidationFailures(FeedOptions.SectionName)`
- **AND** messages SHALL be formatted as `"{section}:{message}"`

#### Scenario: Validator without section prefix

- **WHEN** a validator has existing messages without a section prefix (e.g. DownloadOptionsValidator)
- **THEN** it SHALL use `new ValidationFailures()` (no section argument)
- **AND** messages SHALL be identical to the current output

### Requirement: FeedOptionsValidator

The system SHALL define a `FeedOptionsValidator` class in `FunkArr.Core`
implementing `IValidateOptions<FeedOptions>` that validates all 8 feed
configuration rules currently expressed as inline `.Validate()` lambdas.

#### Scenario: All rules pass

- **WHEN** all feed options are within valid ranges
- **THEN** validation SHALL return `ValidateOptionsResult.Success`

#### Scenario: Single rule fails

- **WHEN** `Interval` is `TimeSpan.Zero`
- **THEN** validation SHALL fail with message `"FunkArr:Feed:Interval must be positive"`

#### Scenario: Multiple rules fail

- **WHEN** `Interval` is `TimeSpan.Zero` and `PageSize` is 0
- **THEN** validation SHALL fail with both messages in a single result

### Requirement: Bind via GetSection consistently

Every options registration SHALL use `Bind(context.Configuration.GetSection(...))`
with the options type's `SectionName` constant. `BindConfiguration()` SHALL NOT
be used.

#### Scenario: EnrichmentSetupContainer normalization

- **WHEN** `EnrichmentSetupContainer` registers `TvdbOptions` and `TmdbOptions`
- **THEN** it SHALL use `Bind(context.Configuration.GetSection(TvdbOptions.SectionName))` and `Bind(context.Configuration.GetSection(TmdbOptions.SectionName))`
- **AND** the `SetupContext` parameter SHALL be named `context`, not `_`

### Requirement: Feed gauge uses Gauge instead of ObservableGauge

`Feed/Telemetry.cs` SHALL use `Gauge<int>` for the `funkarr.feed.entries`
metric, matching the pattern used by Download and RuleSet telemetry.

#### Scenario: Feed entries metric

- **WHEN** the feed store updates its entry count
- **THEN** it SHALL call `Telemetry.Entries.Record(count)` on a `Gauge<int>` field
- **AND** the metric name SHALL remain `funkarr.feed.entries`
