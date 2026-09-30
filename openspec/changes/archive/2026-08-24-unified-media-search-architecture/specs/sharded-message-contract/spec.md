## ADDED Requirements

### Requirement: SearchRequestActor sharded message
`SearchRequestActor` messages SHALL implement `IShardedMessage`. The `Search.Tv`, `Search.Movie`, and `Search.Text` message types SHALL compute `EntityKey` using a type prefix: `"tv:{tvdbId}"`, `"movie:{imdbId}"` or `"movie:q:{query}"`, and `"text:{query}"` respectively.

#### Scenario: TV search EntityKey
- **WHEN** `Search.Tv` is created with tvdbId 329324
- **THEN** `EntityKey` SHALL be `"tv:329324"`

#### Scenario: Movie search EntityKey by IMDB
- **WHEN** `Search.Movie` is created with imdbId "tt0082096"
- **THEN** `EntityKey` SHALL be `"movie:tt0082096"`

#### Scenario: Movie search EntityKey by query
- **WHEN** `Search.Movie` is created with imdbId null and query "Das Boot"
- **THEN** `EntityKey` SHALL be `"movie:q:Das Boot"`

#### Scenario: Text search EntityKey
- **WHEN** `Search.Text` is created with query "Tatort"
- **THEN** `EntityKey` SHALL be `"text:Tatort"`

### Requirement: ShowActor sharded messages
`ShowActor` messages (`ResolveSearch`, `Match`, `ApplyCommunityRules`, `ApplyLocalOverride`, `GetMatchQuality`) SHALL implement `IShardedMessage` with `EntityKey` equal to the tvdbId string.

#### Scenario: ResolveSearch EntityKey
- **WHEN** `ShowActor.ResolveSearch` is created for tvdbId "329324"
- **THEN** `EntityKey` SHALL be `"329324"`

### Requirement: MovieActor sharded messages
`MovieActor` messages (`ResolveSearch`, `Match`, `ApplyCommunityRules`, `ApplyLocalOverride`, `GetMatchQuality`) SHALL implement `IShardedMessage` with `EntityKey` equal to the imdbId string.

#### Scenario: ResolveSearch EntityKey
- **WHEN** `MovieActor.ResolveSearch` is created for imdbId "tt0082096"
- **THEN** `EntityKey` SHALL be `"tt0082096"`

## MODIFIED Requirements

### Requirement: ShardedMessageExtractor
The system SHALL provide a single `ShardedMessageExtractor` class in `FunkArr.Shared` that extracts entity IDs from any `IShardedMessage`. It SHALL accept `maxNumberOfShards` as a constructor parameter.

#### Scenario: Entity ID extraction from IShardedMessage
- **WHEN** a message implementing `IShardedMessage` with `EntityKey = "tv:329324"` is processed by the extractor
- **THEN** the extractor SHALL return `"tv:329324"` as the entity ID

#### Scenario: Non-sharded message returns null
- **WHEN** a message that does not implement `IShardedMessage` is processed by the extractor
- **THEN** the extractor SHALL return `null`

#### Scenario: Different shard counts per region
- **WHEN** `ShardedMessageExtractor` is instantiated with `maxNumberOfShards: 20` for SearchRequest and `maxNumberOfShards: 10` for ShowActor
- **THEN** each instance SHALL use its own shard count for hash distribution

## REMOVED Requirements

### Requirement: MovieSearchActor.Search derives EntityKey
**Reason**: `MovieSearchActor` is replaced by `SearchRequestActor` and `MovieActor`. The EntityKey derivation is now handled by `Search.Movie` and `MovieActor` messages respectively.
**Migration**: `Search.Movie` computes `"movie:{imdbId}"` or `"movie:q:{query}"`. `MovieActor` messages use `imdbId` directly.
