## MODIFIED Requirements

### Requirement: DownloadManagerState tracks Category per entry
DownloadManagerState SHALL store MediaType Category alongside each queued and
dispatched entry. Category SHALL be populated from AddDownload.Category at
enqueue time.

#### Scenario: Category stored on enqueue
- **WHEN** AddDownload is handled
- **THEN** the QueueEntry SHALL include the command's Category

#### Scenario: Category preserved on dispatch
- **WHEN** a queued entry is dispatched
- **THEN** the DispatchedEntry SHALL include the entry's Category

#### Scenario: Old events without Category
- **WHEN** a DownloadEnqueued event without Category field is recovered
- **THEN** Category SHALL default to MediaType.Show

### Requirement: HandleQueryQueue uses pre-pagination
HandleQueryQueue SHALL paginate on ID-level before asking workers. Only
workers for the requested page SHALL be queried.

#### Scenario: Paginated query without category filter
- **WHEN** QueryQueue with Start=20, Limit=20 is received and queue has 50 items
- **THEN** only 20 Ask messages SHALL be sent to the shard region
- **AND** totalItems SHALL be 50

#### Scenario: Paginated query with category filter
- **WHEN** QueryQueue with Category=Movie is received and 10 of 50 items are movies
- **THEN** totalItems SHALL be 10
- **AND** only the page slice of those 10 items SHALL be queried

#### Scenario: Empty queue
- **WHEN** QueryQueue is received and queue is empty
- **THEN** QueueResult SHALL be returned immediately with no Ask calls

### Requirement: HandleQueryQueue uses Akka.Streams pipeline
HandleQueryQueue SHALL use Source.From + SelectAsync for worker queries with
controlled parallelism and ResumingDecider supervision.

#### Scenario: Worker timeout during query
- **WHEN** a worker Ask times out during the stream pipeline
- **THEN** the timed-out element SHALL be skipped (not fail the stream)
- **AND** the result SHALL contain fewer items than the page size

#### Scenario: Parallelism control
- **WHEN** a page of 20 items is queried
- **THEN** at most 8 concurrent Ask messages SHALL be in flight
