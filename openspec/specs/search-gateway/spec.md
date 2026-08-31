## ADDED Requirements

### Requirement: SearchGatewayManager routes search requests by type

The SearchGatewayManager SHALL be a Cluster Singleton actor that receives search requests and routes them to the correct shard region based on the Newznab search type and category.

- `t=tvsearch` → TvSearch ShardRegion
- `t=movie` → MovieSearch ShardRegion
- `t=search` with `cat` in 5xxx range → TvSearch ShardRegion
- `t=search` with `cat` in 2xxx range → MovieSearch ShardRegion
- `t=search` without `cat` → both shard regions (fan-out)

#### Scenario: TV search routing

- **WHEN** a TvSearchCommand is received
- **THEN** the Gateway SHALL generate a SearchId, capture the original Sender, store a PendingSearch entry, and Tell the TvSearch ShardRegion with the SearchId

#### Scenario: Movie search routing

- **WHEN** a MovieSearchCommand is received
- **THEN** the Gateway SHALL generate a SearchId, capture the original Sender, store a PendingSearch entry, and Tell the MovieSearch ShardRegion with the SearchId

#### Scenario: General search with TV category

- **WHEN** a general search with cat in the 5xxx range is received
- **THEN** the Gateway SHALL route to the TvSearch ShardRegion only

#### Scenario: General search with movie category

- **WHEN** a general search with cat in the 2xxx range is received
- **THEN** the Gateway SHALL route to the MovieSearch ShardRegion only

#### Scenario: General search without category (fan-out)

- **WHEN** a general search without a cat parameter is received
- **THEN** the Gateway SHALL send to both TvSearch and MovieSearch shard regions with the same SearchId and track both pending results

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
