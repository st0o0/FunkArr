## Requirements

### Requirement: Sharded actors registered via WithShardRegion

The actor system setup SHALL register `DownloadRequestActor`, `DownloadActor`,
`TextSearchActor`, `TvSearchActor`, and `MovieSearchActor` as Cluster Sharding
entities via `WithShardRegion<T>`. Each registration SHALL use a
`ShardedMessageExtractor` and resolve actor props through the DI-aware
`resolver.Props<T>()` factory.

#### Scenario: Download shard regions

- **WHEN** the actor system starts
- **THEN** `DownloadRequestActor` is registered as ShardRegion with typeName
  `"download-request-tracker"` and 10 shards, and `DownloadActor` is registered
  as ShardRegion with typeName `"download-coordinator"` and 10 shards

#### Scenario: Search shard regions

- **WHEN** the actor system starts
- **THEN** `TextSearchActor`, `TvSearchActor`, and `MovieSearchActor` are each
  registered as ShardRegion with 20 shards and typeNames `"text-search"`,
  `"tv-search"`, and `"movie-search"` respectively

### Requirement: Singleton actors registered via WithResolvableActors

The actor system setup SHALL register `RuleSetActor`, `MediathekGatewayActor`,
`BrowseActor`, `SeriesResolver`, `MovieResolver`, and `QueueActor` via
`WithResolvableActors` with DI-resolved construction. These actors are
resolvable via `Context.GetActorAsync<T>()`.

#### Scenario: Resolvable actor registration

- **WHEN** the actor system starts
- **THEN** `RuleSetActor` (name `"ruleset-registry"`),
  `MediathekGatewayActor` (name `"mediathek-gateway"`),
  `BrowseActor` (name `"browse-coordinator"`),
  `SeriesResolver` (name `"series-resolver"`),
  `MovieResolver` (name `"movie-resolver"`), and
  `QueueActor` (name `"queue-coordinator"`) are registered via
  `WithResolvableActors` and resolvable via `Context.GetActorAsync<T>()`

### Requirement: Actors resolve siblings via Context.GetActorAsync

Actors that depend on other actors SHALL resolve them via
`Context.GetActorAsync<T>().PipeTo(Self)` during `PreStart` instead of receiving
raw `IActorRef` values via constructor injection.

#### Scenario: Actor resolves dependencies on startup

- **WHEN** an actor that depends on another registered actor starts
- **THEN** it calls `Context.GetActorAsync<T>()` with `PipeTo(Self)` to receive
  the resolved refs as messages

#### Scenario: Actor stashes messages until dependencies resolved

- **WHEN** an actor receives a request before its dependencies are resolved
- **THEN** it stashes the message and processes it after all refs are available

#### Scenario: Actor re-resolves on Terminated

- **WHEN** a watched dependency actor terminates
- **THEN** the actor re-initiates `Context.GetActorAsync<T>()` for the
  terminated dependency and transitions back to the resolving phase

### Requirement: API endpoints resolve actors via ActorRegistry

API endpoints SHALL resolve shard regions and singleton actors via the
`IActorRegistry` injected through DI. Sharded actors are addressed by sending
messages through the shard region proxy; singleton actors are resolved by key.

#### Scenario: Newznab endpoint routes search requests

- **WHEN** a Newznab API request arrives
- **THEN** the endpoint resolves the appropriate search shard region
  (`TextSearchActor`, `TvSearchActor`, or `MovieSearchActor`) via the registry

#### Scenario: SABnzbd endpoint resolves QueueActor

- **WHEN** a SABnzbd API request arrives
- **THEN** the endpoint resolves `QueueActor` via the registry

### Requirement: Child actors created via ResolveChildActor

Parent actors that create child actors with DI dependencies SHALL use
`Context.ResolveChildActor<T>()` instead of manual `Props.Create` +
`Context.ActorOf`.

#### Scenario: Parent creates DI-resolved child

- **WHEN** a parent actor needs to create a child actor with DI dependencies
- **THEN** it creates the child via
  `Context.ResolveChildActor<T>(name, args)` with DI-resolved dependencies

### Requirement: Setup container free of manual DI resolution

`FunkArrActorSystemSetup.BuildSystem` SHALL NOT call `GetRequiredService` to
obtain services for actor construction. All actor dependencies SHALL be resolved
via the DI container automatically through `WithResolvableActors` or
`WithShardRegion` prop factories.
Note: `GetRequiredService` for `IOptions<FunkArrOptions>` (configuration) is
acceptable — the restriction applies to actor dependencies, not configuration.

#### Scenario: No GetRequiredService in BuildSystem for actor deps

- **WHEN** the `BuildSystem` method executes
- **THEN** it does not call `serviceProvider.GetRequiredService` for
  `MediathekClient`, `TvdbClient`, `IHttpClientFactory`, `MuxingService`, or
  `IFileService` to pass them to actor constructors
