# Download Priority

## Purpose

Priority enum, priority-aware enqueue behavior, and SABnzbd priority mapping for the download pipeline.

## Requirements

### Requirement: DownloadPriority enum
The system SHALL define a `DownloadPriority` enum in `FunkArr.Messages.Download` with values `Low = -1`, `Normal = 0`, `High = 1`. The integer values SHALL match SABnzbd's priority scale for direct mapping.

#### Scenario: Enum values
- **WHEN** the DownloadPriority enum is inspected
- **THEN** it SHALL contain `Low` (-1), `Normal` (0), `High` (1)

#### Scenario: Default value
- **WHEN** a DownloadPriority is default-initialized
- **THEN** its value SHALL be `Normal` (0)

### Requirement: Priority-aware enqueue
When a download is enqueued with a priority, it SHALL be inserted at the end of its priority bucket in the queue, maintaining the priority sort invariant.

#### Scenario: Enqueue with High priority
- **WHEN** AddDownload is sent with Priority = High
- **THEN** the download SHALL be inserted after existing High items and before any Normal items

#### Scenario: Enqueue with default priority
- **WHEN** AddDownload is sent without specifying Priority
- **THEN** the download SHALL be enqueued with Priority = Normal

#### Scenario: Enqueue into empty queue
- **WHEN** AddDownload is sent to an empty queue with any priority
- **THEN** the download SHALL be the sole entry in the queue

### Requirement: SABnzbd priority mapping
The SABnzbd API SHALL map integer priority values to the DownloadPriority enum or ForceStartDownload action.

#### Scenario: Standard priority mapping
- **WHEN** SABnzbd API receives priority value -1
- **THEN** it SHALL map to `DownloadPriority.Low`
- **WHEN** SABnzbd API receives priority value 0
- **THEN** it SHALL map to `DownloadPriority.Normal`
- **WHEN** SABnzbd API receives priority value 1
- **THEN** it SHALL map to `DownloadPriority.High`

#### Scenario: Force priority mapping
- **WHEN** SABnzbd API receives priority value 2 for an existing queued download
- **THEN** it SHALL trigger `ForceStartDownload` instead of setting a queue priority

#### Scenario: Force priority on addfile
- **WHEN** SABnzbd API receives an addfile request with priority value 2
- **THEN** it SHALL enqueue with `DownloadPriority.High` and then trigger `ForceStartDownload`

#### Scenario: Null or missing priority
- **WHEN** SABnzbd API receives no priority value
- **THEN** it SHALL default to `DownloadPriority.Normal`

### Requirement: SABnzbd queue priority operation
The SABnzbd API SHALL support `mode=queue&name=priority&value={nzo_id}&value2={priority_int}` to change a queued download's priority.

#### Scenario: Set priority via SABnzbd API
- **WHEN** GET `/download/api?mode=queue&name=priority&value={id}&value2=1`
- **THEN** the download's priority SHALL be changed to High

#### Scenario: Force via SABnzbd priority
- **WHEN** GET `/download/api?mode=queue&name=priority&value={id}&value2=2`
- **THEN** `ForceStartDownload` SHALL be triggered for that download

### Requirement: SABnzbd queue switch operation
The SABnzbd API SHALL support `mode=queue&name=switch&value={nzo_id1}&value2={nzo_id2}` to swap two queued downloads.

#### Scenario: Switch via SABnzbd API
- **WHEN** GET `/download/api?mode=queue&name=switch&value={id1}&value2={id2}`
- **THEN** the two downloads SHALL be swapped (same-bucket constraint applies)

### Requirement: SABnzbd addfile reads priority
The SABnzbd API `addfile` POST handler SHALL read the `priority` query parameter and pass it to `AddDownload`.

#### Scenario: Addfile with priority
- **WHEN** POST `/download/api?mode=addfile&priority=1` with an NZB file
- **THEN** the download SHALL be enqueued with `DownloadPriority.High`

#### Scenario: Addfile without priority
- **WHEN** POST `/download/api?mode=addfile` with an NZB file
- **THEN** the download SHALL be enqueued with `DownloadPriority.Normal`

### Requirement: SABnzbd QueueSlot reports actual priority
The SABnzbd API `QueueSlot` response SHALL report the download's actual priority string instead of a hardcoded value.

#### Scenario: Priority in queue response
- **WHEN** a download with High priority appears in the SABnzbd queue response
- **THEN** its `Priority` field SHALL be `"High"` (not hardcoded `"Normal"`)

### Requirement: SABnzbd value2 query parameter
The `DownloadGetRequest` SHALL include a `value2` query parameter (`[FromQuery(Name = "value2")] string? Value2`) for SABnzbd switch and priority operations.

#### Scenario: Value2 parameter parsed
- **WHEN** a SABnzbd request includes `&value2=somevalue`
- **THEN** the `Value2` property SHALL contain `"somevalue"`
