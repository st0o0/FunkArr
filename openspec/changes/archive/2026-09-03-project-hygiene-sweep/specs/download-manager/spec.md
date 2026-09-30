## MODIFIED Requirements

### Requirement: DownloadManager answers queue queries
The DownloadManager SHALL handle `QueryQueue` messages by fanning out `QueryWorkerStatus` to all Workers in its Queued and Dispatched sets, collecting responses, and building a `QueueResult`. The handler SHALL apply the `Category` filter, `Start` offset, and `Limit` from the `QueryQueue` message to the collected responses before building the result.

#### Scenario: Queue query with fan-out
- **WHEN** a `QueryQueue` message is received
- **THEN** the Manager SHALL send `QueryWorkerStatus` to each Worker in the Queued and Dispatched sets via the shard region
- **AND** collect responses with a timeout of 2 seconds
- **AND** respond with a `QueueResult` built from Worker responses
- **AND** Workers that do not respond within the timeout SHALL be represented with zero progress

#### Scenario: Queue query with category filter
- **WHEN** a `QueryQueue` message is received with `Category = "sonarr"`
- **THEN** the Manager SHALL include only items matching the `"sonarr"` category in the result

#### Scenario: Queue query with pagination
- **WHEN** a `QueryQueue` message is received with `Start = 2` and `Limit = 5`
- **THEN** the Manager SHALL skip the first 2 items and return at most 5 items
- **AND** `QueueResult.TotalItems` SHALL reflect the total count after category filtering but before pagination

#### Scenario: Queue query with Limit 0 means all
- **WHEN** a `QueryQueue` message is received with `Limit = 0`
- **THEN** the Manager SHALL return all items (after category filter and start offset)
