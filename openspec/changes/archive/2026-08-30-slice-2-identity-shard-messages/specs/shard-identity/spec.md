## ADDED Requirements

### Requirement: Per-family shard interfaces
The system SHALL define one shard interface per entity family: `ISeriesShard { int Id }`, `IMovieShard { string Id }`, `IDownloadShard { string Id }`, and `ISearchShard { string Id }`. Each interface SHALL reside in `FunkArr.Messages`.

#### Scenario: Series message routing
- **WHEN** a message implements `ISeriesShard` with `Id = 12345`
- **THEN** the series shard extractor SHALL route it to entity `"series-12345"`

#### Scenario: Movie message routing
- **WHEN** a message implements `IMovieShard` with `Id = "tt0133093"`
- **THEN** the movie shard extractor SHALL route it to entity `"movie-tt0133093"`

#### Scenario: Download message routing
- **WHEN** a message implements `IDownloadShard` with `Id = "nzo_abc123"`
- **THEN** the download shard extractor SHALL route it to entity `"nzo_abc123"`

### Requirement: Per-region shard extractors
The system SHALL define one shard message extractor per shard region. Each extractor SHALL read the typed `Id` from its corresponding shard interface and format a readable shard key.

#### Scenario: Extractor rejects wrong interface
- **WHEN** a message that does not implement `ISeriesShard` is sent to the series region
- **THEN** the extractor SHALL not route the message (compile-time: the message type won't implement the interface)

### Requirement: Entity actor typed id constructor
Entity actors SHALL receive their typed id via `entityPropsFactory` instead of parsing `Self.Path.Name`. The id parse SHALL happen once in the factory, not in the actor.

#### Scenario: ShowActor receives int id
- **WHEN** ShowActor is constructed for entity key `"12345"`
- **THEN** the factory SHALL parse it to `int 12345` and pass it to the constructor

### Requirement: IRequest marker interface
The system SHALL define `IRequest<TResponse>` as a marker interface in `FunkArr.Messages`. Messages that expect a typed response SHALL implement it.

#### Scenario: GetRuleSet implements IRequest
- **WHEN** `GetRuleSet` is declared
- **THEN** it SHALL implement `IRequest<RuleSetResponse>`
