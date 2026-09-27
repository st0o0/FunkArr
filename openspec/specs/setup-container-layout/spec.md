# Setup Container Layout

## Purpose

Defines the ownership and ordering of Servus setup containers, ensuring each domain owns its registrations and cross-cutting infrastructure lives in CoreSetupContainer.

## Requirements

### Requirement: Per-domain SetupContainer ownership
Each domain SHALL own a single SetupContainer that registers its DI services, options, actors, health checks, and API endpoints. Cross-cutting infrastructure SHALL live in CoreSetupContainer.

#### Scenario: Domain container registers its actors
- **WHEN** a domain container (e.g. SearchSetupContainer) is invoked
- **THEN** it SHALL register its singleton actors via `WithResolvableActors` and its shard regions via `WithShardRegion`
- **AND** AkkaSetupContainer SHALL NOT register any domain-specific actors

#### Scenario: Domain container registers its options
- **WHEN** a domain needs configuration options (e.g. ScoringOptions)
- **THEN** those options SHALL be bound in the domain's own SetupContainer
- **AND** NOT in another domain's container

#### Scenario: Domain container maps its endpoints
- **WHEN** a domain exposes API endpoints
- **THEN** those endpoints SHALL be mapped in the domain's SetupContainer
- **AND** NOT in ApplicationSetupContainer or another domain's container

### Requirement: CoreSetupContainer owns cross-cutting infrastructure
CoreSetupContainer SHALL register infrastructure shared across domains: FunkArrOptions, PostgresOptions, DataPaths, IDataFiles, IFileSystem, RoutingOptions, IRouteResolver, JSON serializer config, and directory health checks.

#### Scenario: Core registrations
- **WHEN** CoreSetupContainer is invoked
- **THEN** it SHALL register FunkArrOptions, PostgresOptions, DataPaths, IDataFiles, IFileSystem, RoutingOptions with validators, IRouteResolver, and directory health checks
- **AND** it SHALL NOT register any domain-specific services or actors

### Requirement: AkkaSetupContainer owns only actor system infrastructure
AkkaSetupContainer SHALL configure the actor system name, persistence provider, remoting, and clustering. It SHALL NOT register any domain-specific actors.

#### Scenario: Actor system infrastructure only
- **WHEN** AkkaSetupContainer is invoked
- **THEN** it SHALL configure actor system "funkarr", SQL persistence (SQLite or PostgreSQL), remoting, and clustering
- **AND** it SHALL NOT call `WithResolvableActors` or `WithShardRegion` for domain actors

### Requirement: ArrApiSetupContainer owns adapter services
ArrApiSetupContainer SHALL register Newznab and SABnzbd adapter services, ApiKeyFilter, ArrApiClient, and map controller endpoints.

#### Scenario: ArrApi registrations
- **WHEN** ArrApiSetupContainer is invoked
- **THEN** it SHALL register NewznabSearchService, NzbService, SabnzbdQueueService, SabnzbdDownloadService, ApiKeyFilter, ArrApiClient, and map controller routes

### Requirement: ScoringSetupContainer owns scoring and history
ScoringSetupContainer SHALL register ScoringOptions, ScoringHistoryOptions, and scoring/history domain actors.

#### Scenario: Scoring registrations
- **WHEN** ScoringSetupContainer is invoked
- **THEN** it SHALL bind ScoringOptions and ScoringHistoryOptions
- **AND** register ScoringManager singleton, StatsCollector singleton, and HistoryWorker shard region

### Requirement: ServiceSetupContainer is removed
The ServiceSetupContainer SHALL be deleted. Its registrations SHALL be distributed to CoreSetupContainer and domain-specific containers.

#### Scenario: No ServiceSetupContainer in chain
- **WHEN** the AppBuilder chain is constructed in Program.cs
- **THEN** ServiceSetupContainer SHALL NOT appear in the chain

### Requirement: Container chain ordering
The AppBuilder chain in Program.cs SHALL follow the order: Logging, Telemetry, Core, Akka, domain containers (Search, Download, Scoring, RuleSet, Enrichment), ArrApi, Application.

#### Scenario: Infrastructure before domains
- **WHEN** the host boots
- **THEN** LoggingSetupContainer, TelemetrySetupContainer, CoreSetupContainer, and AkkaSetupContainer SHALL execute before any domain container
- **AND** ApplicationSetupContainer SHALL execute last
