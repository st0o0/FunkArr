## MODIFIED Requirements

### Requirement: SABnzbd queue reads from DownloadRequestTracker
The SABnzbd controller SHALL query `QueueCoordinator.GetQueueOrder` for the ordered nzoId list, then fan out `GetStatus` to individual `DownloadRequestTracker` shard entities to build the queue response.

#### Scenario: Queue response built from tracker entities
- **WHEN** `mode=queue` is requested
- **THEN** the controller SHALL ask QueueCoordinator for ordered nzoIds, then ask each tracker entity for status, and assemble the SABnzbd queue JSON from the responses

### Requirement: SABnzbd history reads from DownloadRequestTracker
The SABnzbd controller SHALL query completed/failed tracker entities for history entries instead of asking DownloadQueueActor.

#### Scenario: History response built from tracker entities
- **WHEN** `mode=history` is requested
- **THEN** the controller SHALL ask QueueCoordinator for completed job IDs, then ask each tracker entity for history entry data
