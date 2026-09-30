## MODIFIED Requirements

### Requirement: SearchGatewayManager routes search requests by type

The SearchGatewayManager SHALL be a Cluster Singleton actor that receives search requests and routes them to the correct shard region based on the Newznab search type and category. It SHALL forward Limit and Offset from the incoming command to the worker commands.

- `t=tvsearch` → TvSearch ShardRegion
- `t=movie` → MovieSearch ShardRegion
- `t=search` with `cat` in 5xxx range → TvSearch ShardRegion
- `t=search` with `cat` in 2xxx range → MovieSearch ShardRegion
- `t=search` without `cat` → both shard regions (fan-out)

#### Scenario: TV search routing with pagination

- **WHEN** a TvSearchCommand with Limit=100 and Offset=0 is received
- **THEN** the Gateway SHALL generate a SearchId, create a new TvSearchCommand preserving Limit and Offset, and Tell the TvSearch ShardRegion

#### Scenario: Movie search routing with pagination

- **WHEN** a MovieSearchCommand with Limit=50 and Offset=10 is received
- **THEN** the Gateway SHALL generate a SearchId, create a new MovieSearchCommand preserving Limit and Offset, and Tell the MovieSearch ShardRegion

#### Scenario: General search fan-out with pagination

- **WHEN** a GeneralSearchCommand without cat and with Limit=100 is received
- **THEN** the Gateway SHALL send to both TvSearch and MovieSearch shard regions, each with the original Limit and Offset values
