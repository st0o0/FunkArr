## ADDED Requirements

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

### Requirement: SearchGatewayManager manages sender correlation

The Gateway SHALL maintain a `Dictionary<SearchId, PendingSearch>` in its state to correlate worker responses back to the original sender. No `IActorRef` fields in messages.

#### Scenario: Single search response forwarding

- **WHEN** a SearchCompleted message arrives for a single-type search (TV or Movie only)
- **THEN** the Gateway SHALL look up the PendingSearch by SearchId, Tell the OriginalSender with a SearchResult, and remove the PendingSearch entry

#### Scenario: Fan-out merge — both results arrive

- **WHEN** both TV and Movie SearchCompleted messages arrive for a fan-out search
- **THEN** the Gateway SHALL merge both result sets and Tell the OriginalSender with the combined SearchResult

#### Scenario: Fan-out merge — partial result on timeout

- **WHEN** only one of two expected results arrives before the search timeout
- **THEN** the Gateway SHALL respond with whatever results arrived, Tell the OriginalSender, and remove the PendingSearch entry

### Requirement: SearchGatewayManager handles search failures

The Gateway SHALL handle SearchFailed messages and timeouts gracefully.

#### Scenario: Worker responds with SearchFailed

- **WHEN** a SearchFailed message arrives for a pending search
- **THEN** the Gateway SHALL forward the failure to the OriginalSender and remove the PendingSearch entry

#### Scenario: Search timeout

- **WHEN** a PendingSearch exceeds the configurable timeout (default 30 seconds)
- **THEN** the Gateway SHALL respond to the OriginalSender with a SearchFailed containing a timeout reason and remove the PendingSearch entry
