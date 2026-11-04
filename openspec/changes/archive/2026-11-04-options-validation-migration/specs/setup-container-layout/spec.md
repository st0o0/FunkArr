# Setup Container Layout — Delta

## MODIFIED Requirements

### Requirement: CoreSetupContainer owns cross-cutting infrastructure
CoreSetupContainer SHALL register infrastructure shared across domains: FunkArrOptions, PostgresOptions, DataPaths, IDataFiles, IFileSystem, RoutingOptions, IRouteResolver, JSON serializer config, and directory health checks. Options SHALL be registered via `AddOptionsWithValidateOnStart<T>()` or `AddOptionsWithValidateOnStart<T, TValidator>()`.

#### Scenario: Core registrations
- **WHEN** CoreSetupContainer is invoked
- **THEN** it SHALL register `FunkArrOptions` via `AddOptionsWithValidateOnStart<FunkArrOptions>().Bind(...)`
- **AND** `PostgresOptions` via `AddOptionsWithValidateOnStart<PostgresOptions>().Bind(...)`
- **AND** `RoutingOptions` via `AddOptionsWithValidateOnStart<RoutingOptions, RoutingOptionsValidator>().Bind(...)`
- **AND** it SHALL NOT contain a separate `AddSingleton<IValidateOptions<RoutingOptions>>` call

### Requirement: Per-domain SetupContainer ownership
Each domain SHALL own a single SetupContainer that registers its DI services, options, actors, health checks, and API endpoints. Cross-cutting infrastructure SHALL live in CoreSetupContainer.

#### Scenario: Domain container registers its options
- **WHEN** a domain needs configuration options (e.g. ScoringOptions)
- **THEN** those options SHALL be registered via `AddOptionsWithValidateOnStart<T>()` or `AddOptionsWithValidateOnStart<T, TValidator>()` in the domain's own SetupContainer
- **AND** NOT in another domain's container
