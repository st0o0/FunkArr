## Purpose

Ensure every Options class has a startup validator and all Options access goes through the DI pipeline.

## Requirements

### Requirement: Every Options class has a validator
Every Options class registered via `AddOptionsWithValidateOnStart` SHALL have a corresponding `IValidateOptions<T>` implementation registered via the two-type-parameter overload `AddOptionsWithValidateOnStart<T, V>()`.

#### Scenario: VariantOptions validated on startup
- **WHEN** the application starts with an invalid `VariantOptions` configuration
- **THEN** startup SHALL fail with a validation error before the application accepts requests

#### Scenario: TvdbOptions validated on startup
- **WHEN** the application starts with an invalid `TvdbOptions` configuration (e.g. empty API key)
- **THEN** startup SHALL fail with a validation error

#### Scenario: TmdbOptions validated on startup
- **WHEN** the application starts with an invalid `TmdbOptions` configuration (e.g. empty API key)
- **THEN** startup SHALL fail with a validation error

### Requirement: No manual Options binding
Options classes SHALL NOT be instantiated manually via `new T()` + `configuration.Bind()`. All Options access MUST go through the DI-registered `IOptions<T>` / `IOptionsMonitor<T>` / `IOptionsSnapshot<T>` pipeline.

#### Scenario: DownloadServiceExtensions uses DI for RoutingOptions
- **WHEN** `DownloadServiceExtensions` configures named HttpClients based on route definitions
- **THEN** it SHALL access `RoutingOptions` through the DI container, not via manual `new RoutingOptions()` + `Bind()`
