## MODIFIED Requirements

### Requirement: SABnzbd addfile enqueues download
The SABnzbd controller SHALL route `mode=addfile` through `QueueCoordinator.Enqueue` instead of directly telling `DownloadQueueActor.EnqueueDownload`. The controller SHALL receive the nzoId from QueueCoordinator's reply.

#### Scenario: Addfile routed through QueueCoordinator
- **WHEN** the SABnzbd controller receives `mode=addfile` with a download URL and title
- **THEN** it SHALL ask `QueueCoordinator` with `Enqueue(url, title, subtitleUrl)` and receive the generated nzoId

### Requirement: SABnzbd delete routes through QueueCoordinator
The SABnzbd controller SHALL route delete operations through `QueueCoordinator.Cancel` instead of directly modifying download state.

#### Scenario: Delete routed through QueueCoordinator
- **WHEN** the SABnzbd controller receives a delete request for nzoId "abc123"
- **THEN** it SHALL tell `QueueCoordinator` with `Cancel("abc123")`
