## ADDED Requirements

### Requirement: Non-supervised actors registered via WithResolvableActors

The actor system setup SHALL register `SearchActor`, `RuleSetRegistryActor`, and
`MatchLedgerActor` via `WithResolvableActors` with DI-resolved construction. The setup
container SHALL NOT manually create `Props` or call `system.ActorOf` for these actors.

#### Scenario: Resolvable actor registration

- **WHEN** the actor system starts
- **THEN** `SearchActor`, `RuleSetRegistryActor`, and `MatchLedgerActor` are
  registered via `WithResolvableActors` and resolvable via
  `Context.GetActorAsync<T>()`

### Requirement: Supervised actors registered via RegisterWithBackoff helper

The actor system setup SHALL register `DownloadQueueActor` via a static
`RegisterWithBackoff<TActor>` helper method that wraps the actor in a
`BackoffSupervisor` and registers the supervisor ref in the `IActorRegistry`.

#### Scenario: BackoffSupervisor registration

- **WHEN** the actor system starts
- **THEN** `DownloadQueueActor` is wrapped in a `BackoffSupervisor` and registered
  in the `IActorRegistry` under the `DownloadQueueActor` key

### Requirement: Actors resolve siblings via Context.GetActorAsync

Actors that depend on other actors SHALL resolve them via
`Context.GetActorAsync<T>().PipeTo(Self)` during `PreStart` instead of receiving
raw `IActorRef` values via constructor injection.

#### Scenario: SearchActor resolves dependencies on startup

- **WHEN** `SearchActor` starts
- **THEN** it calls `Context.GetActorAsync<RuleSetRegistryActor>()` and
  `Context.GetActorAsync<MatchLedgerActor>()` with `PipeTo(Self)` to receive the
  resolved refs as messages

#### Scenario: SearchActor stashes messages until dependencies resolved

- **WHEN** `SearchActor` receives a search request before both dependencies are
  resolved
- **THEN** it stashes the message and processes it after both refs are available

#### Scenario: SearchActor re-resolves on Terminated

- **WHEN** a watched dependency actor terminates (e.g., after BackoffSupervisor
  restart)
- **THEN** `SearchActor` re-initiates `Context.GetActorAsync<T>()` for the
  terminated dependency and transitions back to the resolving phase

### Requirement: API endpoints resolve actors via ActorRegistry.Get

All Minimal API endpoint classes SHALL resolve actors via `ActorRegistry.Get<T>()`
injected through constructor/parameter DI, replacing `IRequiredActor<T>`.

#### Scenario: Newznab endpoint resolves SearchActor

- **WHEN** a Newznab API request arrives
- **THEN** the endpoint handler resolves `SearchActor` via `ActorRegistry.Get<SearchActor>()`

#### Scenario: SABnzbd endpoint resolves DownloadQueueActor

- **WHEN** a SABnzbd API request arrives
- **THEN** the endpoint handler resolves `DownloadQueueActor` via
  `ActorRegistry.Get<DownloadQueueActor>()`

#### Scenario: Match Intelligence endpoint resolves MatchLedgerActor

- **WHEN** a Match Intelligence API request arrives
- **THEN** the endpoint handler resolves `MatchLedgerActor` via
  `ActorRegistry.Get<MatchLedgerActor>()`

### Requirement: Child actors created via ResolveChildActor

Parent actors that create child actors with DI dependencies SHALL use
`Context.ResolveChildActor<T>()` instead of manual `Props.Create` +
`Context.ActorOf`.

#### Scenario: RuleSetRegistryActor creates RuleSetGeneratorActor children

- **WHEN** `RuleSetRegistryActor` needs to generate a rule set
- **THEN** it creates a `RuleSetGeneratorActor` child via
  `Context.ResolveChildActor<RuleSetGeneratorActor>(name, args)` with DI-resolved
  dependencies

### Requirement: Setup container free of manual DI resolution

`FunkArrActorSystemSetup.BuildSystem` SHALL NOT call `GetRequiredService` to obtain
services for actor construction. All actor dependencies SHALL be resolved via the DI
container automatically through `WithResolvableActors` or `RegisterWithBackoff`.

#### Scenario: No GetRequiredService in BuildSystem for actor deps

- **WHEN** the `BuildSystem` method executes
- **THEN** it does not call `serviceProvider.GetRequiredService` for
  `MediathekClient`, `TvdbClient`, `IHttpClientFactory`, `MuxingService`, or
  `IFileService` to pass them to actor constructors
