## MODIFIED Requirements

### Requirement: SearchGatewayManager routes search requests by type

The SearchGatewayManager SHALL be a Cluster Singleton actor that receives search requests and routes them to the correct shard region based on the search type. It SHALL receive a unified `SearchCommand` and determine routing by pattern matching on `Params` (ISearchParams). It SHALL forward Limit and Offset from the incoming command to the worker commands.

- `Params is TvParams` → TvSearch ShardRegion
- `Params is MovieParams` → MovieSearch ShardRegion
- `Params is null` with `Cat` in 5xxx range → TvSearch ShardRegion
- `Params is null` with `Cat` in 2xxx range → MovieSearch ShardRegion
- `Params is null` without matching `Cat` → both shard regions (fan-out)

#### Scenario: TV search routing with pagination

- **WHEN** a SearchCommand with Params=TvParams(...), Limit=100 and Offset=0 is received
- **THEN** the Gateway SHALL generate a SearchId, create a TvSearchCommand preserving Limit, Offset, and TvParams fields, and Tell the TvSearch ShardRegion

#### Scenario: Movie search routing with pagination

- **WHEN** a SearchCommand with Params=MovieParams(...), Limit=50 and Offset=10 is received
- **THEN** the Gateway SHALL generate a SearchId, create a MovieSearchCommand preserving Limit, Offset, and MovieParams fields, and Tell the MovieSearch ShardRegion

#### Scenario: General search with TV category

- **WHEN** a SearchCommand with Params=null and Cat in the 5xxx range is received
- **THEN** the Gateway SHALL route to the TvSearch ShardRegion only

#### Scenario: General search with movie category

- **WHEN** a SearchCommand with Params=null and Cat in the 2xxx range is received
- **THEN** the Gateway SHALL route to the MovieSearch ShardRegion only

#### Scenario: General search fan-out with pagination

- **WHEN** a SearchCommand with Params=null, no matching Cat, and Limit=100 is received
- **THEN** the Gateway SHALL send to both TvSearch and MovieSearch shard regions, each with the original Limit and Offset values
