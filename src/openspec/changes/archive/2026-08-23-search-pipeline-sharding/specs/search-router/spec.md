# SearchRouter

Stateless singleton actor that receives search requests and forwards them to the appropriate ShardRegion based on search type. Registered via ActorRegistry as SearchRouter.

## ADDED Requirements

### Requirement: Route TV search requests

SearchRouter SHALL accept `TvSearchRequest` messages and forward them to the TvSearchPipeline ShardRegion using `tvdbId` as the entity key.

#### Scenario: TV search request routing
- **WHEN** SearchRouter receives a `TvSearchRequest` with a `tvdbId`
- **THEN** it SHALL forward the request to the TvSearchPipeline ShardRegion with `tvdbId` as the entity key

### Requirement: Route movie search requests

SearchRouter SHALL accept `MovieSearchRequest` messages and forward them to the MovieSearchPipeline ShardRegion using `imdbId` as the entity key when present, or `query` when `imdbId` is absent.

#### Scenario: Movie search request routing with imdbId
- **WHEN** SearchRouter receives a `MovieSearchRequest` with an `imdbId`
- **THEN** it SHALL forward the request to the MovieSearchPipeline ShardRegion with `imdbId` as the entity key

#### Scenario: Movie search request routing with query fallback
- **WHEN** SearchRouter receives a `MovieSearchRequest` without an `imdbId` but with a `query`
- **THEN** it SHALL forward the request to the MovieSearchPipeline ShardRegion with `query` as the entity key

### Requirement: Route text search requests

SearchRouter SHALL accept `TextSearchRequest` messages and forward them to the TextSearchPipeline ShardRegion using `query` as the entity key.

#### Scenario: Text search request routing
- **WHEN** SearchRouter receives a `TextSearchRequest` with a `query`
- **THEN** it SHALL forward the request to the TextSearchPipeline ShardRegion with `query` as the entity key

### Requirement: Stateless design

SearchRouter MUST NOT maintain any state, cache, or pipeline logic. It SHALL act purely as a routing layer.

#### Scenario: No state retention
- **WHEN** SearchRouter forwards a request to a ShardRegion
- **THEN** it MUST NOT store or cache the request or its result

### Requirement: Actor registration

SearchRouter SHALL be registered in ActorRegistry as `SearchRouter` and MUST be started as a singleton actor during application setup.

#### Scenario: Registration at startup
- **WHEN** the actor system starts
- **THEN** SearchRouter SHALL be registered in ActorRegistry and be resolvable by name `SearchRouter`
